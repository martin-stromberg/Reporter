// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Reporter.E2ETests;

/// <summary>
/// Process-lifecycle guard for the <c>Reporter.exe</c> instances started by the
/// suite. Every tracked process is assigned to a process-wide Windows job
/// object configured with <c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c>, so the OS
/// terminates the app even when the test host dies hard and
/// <see cref="ReporterAppFixture.DisposeAsync"/> never runs. In addition
/// <see cref="KillAndWaitAsync(Process, TimeSpan?)"/> hardens the regular
/// teardown: tree kill plus a timeout-bounded exit verification that reports
/// instead of silently swallowing a surviving process.
/// </summary>
internal static class E2EProcessGuard
{
    private static readonly TimeSpan DefaultKillTimeout = TimeSpan.FromSeconds(30);

    // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE — all processes assigned to the job
    // are terminated when the last job handle closes, i.e. when the test host
    // process exits for any reason, including a hard kill.
    private const uint KillOnJobClose = 0x2000;
    private const int ExtendedLimitInformationClass = 9;

    // The handle must stay open until the test host exits; the OS closes it on
    // process exit, which is exactly when KILL_ON_JOB_CLOSE takes effect.
    private static readonly Lazy<IntPtr> Job = new(CreateKillOnCloseJob);

    /// <summary>
    /// Assigns a started app process to the kill-on-close job. Best effort: a
    /// failing assignment (e.g. the test host already runs inside a restrictive
    /// job) is reported but never throws — teardown and the script cleanup
    /// remain as the second line of defense.
    /// </summary>
    /// <param name="process">The started <c>Reporter.exe</c> process.</param>
    public static void TrackProcess(Process process)
    {
        // Capture the PID up front: a Process object whose association was
        // lost throws on Id/Handle access, and the guard must never throw —
        // reading it inside a catch block would throw again.
        var processId = TryGetProcessId(process);

        try
        {
            var job = Job.Value;
            if (job == IntPtr.Zero)
            {
                Console.WriteLine("[E2E] No kill-on-close job available — the app process is not guarded against a hard test host abort.");
                return;
            }

            if (!AssignProcessToJobObject(job, process.Handle))
            {
                Console.WriteLine(
                    $"[E2E] AssignProcessToJobObject failed for Reporter.exe ({Describe(processId)}, error {Marshal.GetLastWin32Error()}) — the process is not guarded against a hard test host abort.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[E2E] AssignProcessToJobObject failed for Reporter.exe ({Describe(processId)}): {ex.Message}");
        }
    }

    /// <summary>
    /// Kills the whole process tree of <paramref name="process"/> and waits up
    /// to <paramref name="timeout"/> for the exit to be confirmed.
    /// </summary>
    /// <param name="process">The process to kill.</param>
    /// <param name="timeout">An optional exit-confirmation timeout.</param>
    /// <returns><see langword="true"/> when the exit was confirmed.</returns>
    public static async Task<bool> KillAndWaitAsync(Process process, TimeSpan? timeout = null)
    {
        var wait = timeout ?? DefaultKillTimeout;

        // Capture the PID before any kill/wait attempt: a Process object that
        // lost its association throws on Id access — reading it inside a
        // catch block would throw again and mask the actual teardown result.
        var processId = TryGetProcessId(process);

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[E2E] Kill of Reporter.exe ({Describe(processId)}) failed: {ex.Message}");
        }

        try
        {
            await process.WaitForExitAsync().WaitAsync(wait).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"[E2E] Reporter.exe ({Describe(processId)}) did not exit within {wait} after Kill — the process may be left running.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[E2E] Could not confirm the exit of Reporter.exe ({Describe(processId)}): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Kills the whole process tree of the process identified by
    /// <paramref name="processId"/> and waits for its exit. Used when only the
    /// PID is reliable (FlaUI may replace the internal <see cref="Process"/>
    /// object of the attached application).
    /// </summary>
    /// <param name="processId">The process ID to kill.</param>
    /// <param name="timeout">An optional exit-confirmation timeout.</param>
    /// <returns><see langword="true"/> when the exit was confirmed.</returns>
    public static async Task<bool> KillAndWaitAsync(int processId, TimeSpan? timeout = null)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            // No such process — already exited.
            return true;
        }
        catch (InvalidOperationException)
        {
            // The process object exists but has already exited.
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[E2E] Could not inspect Reporter.exe (PID {processId}): {ex.Message}");
            return false;
        }

        using (process)
        {
            return await KillAndWaitAsync(process, timeout).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Best-effort recursive delete of a suite temp directory. Retries briefly
    /// because a just-killed app process can keep files (e.g. the SQLite
    /// database) locked for a moment even after its exit was confirmed.
    /// Never throws — a leftover temp directory is tolerable, failing
    /// teardown is not.
    /// </summary>
    /// <param name="path">The directory to delete recursively.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task TryDeleteDirectoryAsync(string path)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }

                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }
        }
    }

    private static IntPtr CreateKillOnCloseJob()
    {
        var handle = CreateJobObject(IntPtr.Zero, null);
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine($"[E2E] CreateJobObject failed (error {Marshal.GetLastWin32Error()}).");
            return IntPtr.Zero;
        }

        try
        {
            var info = new JobObjectExtendedLimitInformation
            {
                BasicLimitInformation = new JobObjectBasicLimitInformation
                {
                    LimitFlags = KillOnJobClose,
                },
            };

            var length = Marshal.SizeOf(info);
            var infoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, infoPtr, false);
                if (!SetInformationJobObject(handle, ExtendedLimitInformationClass, infoPtr, (uint)length))
                {
                    Console.WriteLine($"[E2E] SetInformationJobObject(KILL_ON_JOB_CLOSE) failed (error {Marshal.GetLastWin32Error()}).");
                    CloseHandle(handle);
                    return IntPtr.Zero;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
            }

            return handle;
        }
        catch (Exception ex)
        {
            // A setup failure must not escape: Lazy<T> would cache the
            // exception and poison every later TrackProcess call, and the
            // job handle would leak.
            Console.WriteLine($"[E2E] Could not configure the kill-on-close job: {ex.Message}");
            CloseHandle(handle);
            return IntPtr.Zero;
        }
    }

    private static int? TryGetProcessId(Process process)
    {
        try
        {
            return process.Id;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Describe(int? processId)
    {
        return processId is int pid ? $"PID {pid}" : "unassociated process";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr jobAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr job,
        int jobObjectInformationClass,
        IntPtr jobObjectInformation,
        uint jobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
