// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;

namespace Reporter.E2ETests;

/// <summary>
/// Contains regression tests for <see cref="E2EProcessGuard"/>: the guard is
/// the last line of defense in teardown paths and must never throw itself,
/// even when handed a <see cref="Process"/> object that lost its association.
/// These are plain process-API tests — they deliberately carry no
/// <c>Category=E2E</c> trait so the regular test run covers them.
/// </summary>
public sealed class E2EProcessGuardTests
{
    /// <summary>
    /// A <see cref="Process"/> object that was never associated (or lost its
    /// association, e.g. after FlaUI disposed it) must not make the guard
    /// throw — before the fix the catch block accessed <c>process.Id</c>,
    /// which throws <see cref="InvalidOperationException"/> and masks the
    /// actual test result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task KillAndWaitAsync_UnassociatedProcess_DoesNotThrow()
    {
        using var process = new Process();

        var exited = await E2EProcessGuard.KillAndWaitAsync(process, TimeSpan.FromSeconds(5));

        Assert.False(exited);
    }

    /// <summary>
    /// A PID whose process no longer exists counts as a confirmed exit.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task KillAndWaitAsync_NonexistentProcessId_ConfirmsExit()
    {
        var exited = await E2EProcessGuard.KillAndWaitAsync(NonexistentProcessId(), TimeSpan.FromSeconds(5));

        Assert.True(exited);
    }

    /// <summary>
    /// A process that already exited on its own is reported as confirmed
    /// without a kill attempt.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task KillAndWaitAsync_ExitedProcess_ConfirmsExit()
    {
        using var process = StartProcess("cmd.exe", "/c exit 0");
        process.WaitForExit();

        var exited = await E2EProcessGuard.KillAndWaitAsync(process, TimeSpan.FromSeconds(10));

        Assert.True(exited);
    }

    /// <summary>
    /// A still-running process tree is killed and its exit is confirmed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task KillAndWaitAsync_RunningProcessTree_KillsAndConfirmsExit()
    {
        using var process = StartProcess("cmd.exe", "/c ping -n 60 127.0.0.1 > nul");
        Assert.False(process.HasExited);

        var exited = await E2EProcessGuard.KillAndWaitAsync(process, TimeSpan.FromSeconds(15));

        Assert.True(exited);
        Assert.True(process.HasExited);
    }

    /// <summary>
    /// <see cref="E2EProcessGuard.TrackProcess"/> is documented as
    /// best-effort that never throws — a <see cref="Process"/> object whose
    /// handle can no longer be opened must not escape the method either.
    /// </summary>
    [Fact]
    public void TrackProcess_UnassociatedProcess_DoesNotThrow()
    {
        using var process = new Process();

        var exception = Record.Exception(() => E2EProcessGuard.TrackProcess(process));

        Assert.Null(exception);
    }

    /// <summary>
    /// A populated temp directory is removed recursively.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDeleteDirectoryAsync_ExistingDirectory_RemovesIt()
    {
        var directory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory, "reporter.db"), "x");

        await E2EProcessGuard.TryDeleteDirectoryAsync(directory);

        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// A missing directory is tolerated without throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDeleteDirectoryAsync_MissingDirectory_DoesNotThrow()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"reporter-e2e-guard-test-{Guid.NewGuid():N}");

        await E2EProcessGuard.TryDeleteDirectoryAsync(directory);
    }

    /// <summary>
    /// A file that is still locked when the first delete attempt runs — the
    /// observed case of a just-killed app releasing its database a moment
    /// after the confirmed exit — is removed by a retry instead of leaving a
    /// temp leftover behind.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDeleteDirectoryAsync_BrieflyLockedFile_RetriesAndRemoves()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "reporter.db");
        var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        var deleteTask = E2EProcessGuard.TryDeleteDirectoryAsync(directory);
        await Task.Delay(500);
        stream.Dispose();
        await deleteTask;

        Assert.False(Directory.Exists(directory));
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"reporter-e2e-guard-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static int NonexistentProcessId()
    {
        using var process = StartProcess("cmd.exe", "/c exit 0");
        var processId = process.Id;
        process.WaitForExit();
        return processId;
    }

    private static Process StartProcess(string fileName, string arguments)
    {
        return Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
    }
}
