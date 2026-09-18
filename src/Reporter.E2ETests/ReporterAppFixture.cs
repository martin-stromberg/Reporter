// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Reporter.E2ETests;

/// <summary>
/// Collection fixture that owns the whole E2E environment: it starts the
/// <see cref="StubFeedServer"/>, launches a previously built <c>Reporter.exe</c>
/// with <c>REPORTER_FEEDSEARCH_ENDPOINT</c> pointing at the stub and
/// <c>REPORTER_DB_PATH</c> pointing at a temp database, attaches FlaUI (UIA3) to
/// the main window and tears everything down after the suite.
/// </summary>
public sealed class ReporterAppFixture : IAsyncLifetime
{
    private string? _tempDirectory;
    private int? _appProcessId;

    /// <summary>
    /// Gets the stub web server the app under test talks to.
    /// </summary>
    /// <value>The running <see cref="StubFeedServer"/> instance.</value>
    public StubFeedServer Server { get; } = new();

    /// <summary>
    /// Gets the FlaUI application wrapper of the running <c>Reporter.exe</c>.
    /// </summary>
    public Application App { get; private set; } = null!;

    /// <summary>
    /// Gets the UIA3 automation instance.
    /// </summary>
    public UIA3Automation Automation { get; private set; } = null!;

    /// <summary>
    /// Gets the application's main window.
    /// </summary>
    public Window MainWindow { get; private set; } = null!;

    /// <summary>
    /// Gets the path of the isolated SQLite database the app writes to.
    /// </summary>
    public string DatabasePath { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await Server.InitializeAsync().ConfigureAwait(false);
        var process = StartAppProcess();

        try
        {
            await AttachAndWaitForMainWindowAsync(process).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Every failure after Process.Start must not leave the app behind —
            // on paths before the App assignment DisposeAsync cannot see the
            // process at all.
            try
            {
                await KillAppProcessAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Best effort — the original failure must not be masked.
            }

            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Re-resolves the application's main window. Called by the tests instead of
    /// caching <see cref="MainWindow"/> so stale UIA proxies cannot break a whole
    /// test after a dialog or popup phase. Throws when the app exited.
    /// </summary>
    /// <returns>The current main window.</returns>
    public Window GetMainWindow()
    {
        if (App.HasExited)
        {
            throw new InvalidOperationException("The Reporter.exe process is not running anymore.");
        }

        try
        {
            return App.GetMainWindow(Automation)
                ?? throw new InvalidOperationException("Reporter.exe has no main window.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not resolve the Reporter.exe main window.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await KillAppProcessAsync().ConfigureAwait(false);

        App?.Dispose();
        Automation?.Dispose();

        await Server.DisposeAsync().ConfigureAwait(false);

        if (_tempDirectory is not null)
        {
            // Best effort with brief retries — a just-killed app can keep the
            // database file locked for a moment after its confirmed exit.
            await E2EProcessGuard.TryDeleteDirectoryAsync(_tempDirectory).ConfigureAwait(false);
        }
    }

    // REPORTER_APP_PATH wins when it points at an existing file; otherwise the
    // Debug build output convention is used by walking up from the test output
    // directory until the repository root (containing src/Reporter) is found.
    // Internal so DemoSeedTests can reuse the resolution for its own instance.
    internal static string ResolveAppPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("REPORTER_APP_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src", "Reporter", "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", "Reporter.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Reporter.exe not found. Build the app first (see scripts/Run-E2ETests.ps1) " +
            "or set REPORTER_APP_PATH to a built executable.",
            overridePath);
    }

    // Creates the isolated temp directory plus database path and launches
    // Reporter.exe against them. Records the started PID in _appProcessId and
    // assigns the process to the kill-on-close job.
    private Process StartAppProcess()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"reporter-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        DatabasePath = Path.Combine(_tempDirectory, "reporter.db");

        var appPath = ResolveAppPath();
        var startInfo = new ProcessStartInfo(appPath) { UseShellExecute = false };
        startInfo.Environment["REPORTER_FEEDSEARCH_ENDPOINT"] = Server.DirectoryUrl;
        startInfo.Environment["REPORTER_DB_PATH"] = DatabasePath;
        // The fresh temp database would trigger the first-run demo seed: the
        // extra "News" category and feed card plus the real apple.com request
        // would break the hermetic smoke tests, so the seed is disabled here.
        startInfo.Environment["REPORTER_DISABLE_DEMO_SEED"] = "1";
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{appPath}'.");
        _appProcessId = process.Id;
        E2EProcessGuard.TrackProcess(process);
        return process;
    }

    // Attaches FlaUI to the started app, registers the exit watcher and waits
    // for the main window including the post-attach settle pause.
    private async Task AttachAndWaitForMainWindowAsync(Process process)
    {
        Automation = new UIA3Automation();
        // NB: FlaUI replaces the Process object inside Application when
        // waiting for the main window, so the process is only managed
        // through App.
        App = Application.Attach(process);

        // Records when and how the app process exits — the app should
        // never exit on its own during a suite, so this marker pinpoints
        // unexpected exits.
        _ = Task.Run(async () =>
        {
            try
            {
                using var watcher = Process.GetProcessById(App.ProcessId);
                await watcher.WaitForExitAsync().ConfigureAwait(false);
                Console.WriteLine($"[E2E] Reporter.exe exited at {DateTime.Now:HH:mm:ss.fff} with code {watcher.ExitCode}");
            }
            catch (Exception)
            {
                // The process object may already be gone.
            }
        });

        // The first start runs the database migration, so the wait is generous.
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromMinutes(2))
            ?? throw new InvalidOperationException(
                "Reporter.exe did not show a main window within two minutes. " +
                "The suite requires an interactive Windows desktop session.");

        // Fixed pause after the successful attach: gives the user a moment
        // to stop any ongoing interaction before the UIA automation takes
        // over focus and input.
        await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

        MainWindow.SetForeground();
    }

    // Tree kill with exit confirmation — the guard reports a surviving
    // process on the console instead of it being swallowed silently. The PID
    // recorded at Process.Start is the fallback for the cases where FlaUI
    // replaced its internal Process object or App was never assigned.
    private async Task KillAppProcessAsync()
    {
        int? processId;
        try
        {
            processId = App?.ProcessId ?? _appProcessId;
        }
        catch (Exception)
        {
            processId = _appProcessId;
        }

        if (processId is not null)
        {
            await E2EProcessGuard.KillAndWaitAsync(processId.Value).ConfigureAwait(false);
        }
    }
}
