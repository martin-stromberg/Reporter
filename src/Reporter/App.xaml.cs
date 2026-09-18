// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data;

namespace Reporter;

/// <summary>
/// Represents the cross-platform .NET MAUI application.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;
    private IDebugLogService? _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="services">The application's service provider.</param>
    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        Resources.MergedDictionaries.Add(new Resources.Styles.Colors());
        Resources.MergedDictionaries.Add(new Resources.Styles.Styles());
    }

    /// <summary>
    /// Initializes the application and applies pending database migrations.
    /// </summary>
    protected override async void OnStart()
    {
        base.OnStart();

        using var scope = _services.CreateScope();
        var debugLogService = scope.ServiceProvider.GetRequiredService<IDebugLogService>();
        _debugLogService = debugLogService;

        await RunStartupStepAsync(() => MigrateDatabaseAsync(scope), "Database migration failed on start", debugLogService);

        await debugLogService.BeginSessionAsync();

        RunStartupStep(() => ExcludeDatabaseFilesFromBackup(scope), "Backup exclusion failed on start", debugLogService);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        await RunStartupStepAsync(() => SeedDemoContentAsync(scope), "Demo content seed failed", debugLogService);
        await RunStartupStepAsync(() => CleanupRetainedDataAsync(scope), "Retention cleanup failed on start", debugLogService);
        await RunStartupStepAsync(() => ApplyThemeAsync(scope), "Theme apply failed on start", debugLogService);
        RunStartupStep(() => StartNetworkMonitoring(scope), "Network status init failed on start", debugLogService);
        await RunStartupStepAsync(() => StartAutoRefreshAsync(scope), "Auto refresh start failed", debugLogService);
    }

    /// <inheritdoc />
    protected override void OnSleep()
    {
        base.OnSleep();
        _ = _debugLogService?.LogAsync(DebugLogCategory.Lifecycle, "App suspended", level: DebugLogLevel.Info);
    }

    /// <inheritdoc />
    protected override void OnResume()
    {
        base.OnResume();
        _ = _debugLogService?.LogAsync(DebugLogCategory.Lifecycle, "App resumed", level: DebugLogLevel.Info);
    }

    /// <summary>
    /// Creates the application's main window.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>The main application window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_services.GetRequiredService<AppShell>());

        if (OperatingSystem.IsWindows())
        {
            // Fenster standardmaessig in Smartphone-Groesse starten, damit
            // mobile UI-Probleme auf dem Windows-Target direkt sichtbar werden.
            window.Width = 390;
            window.Height = 844;
        }

        return window;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Bewusste Einschraenkung: Das LogAsync laeuft fire-and-forget, weil ein
        // blockierendes Warten im Absturzpfad riskanter waere — bei einem harten
        // Absturz kann der letzte Eintrag verloren gehen.
        var details = e.ExceptionObject is Exception exception ? exception.ToString() : e.ExceptionObject?.ToString();
        _ = _debugLogService?.LogAsync(DebugLogCategory.Exception, "Unhandled exception", details, DebugLogLevel.Error);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Fire-and-forget wie in OnUnhandledException — siehe Kommentar dort.
        _ = _debugLogService?.LogAsync(DebugLogCategory.Exception, "Unobserved task exception", e.Exception.ToString(), DebugLogLevel.Error);
    }

    private static async Task MigrateDatabaseAsync(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<ReporterDbContext>();
        await context.Database.MigrateAsync();
    }

    private static void ExcludeDatabaseFilesFromBackup(IServiceScope scope)
    {
        var databasePath = scope.ServiceProvider.GetRequiredService<DatabasePath>();
        var backupExclusionService = scope.ServiceProvider.GetRequiredService<IBackupExclusionService>();
        backupExclusionService.ExcludeFromBackup(databasePath.FilePath);

        // Die WAL-/SHM-Sidecar-Dateien gehoeren zur Datenbank und duerfen
        // ebenfalls nicht ins iCloud-Backup laufen; fehlende Dateien
        // behandelt der Service als No-op.
        backupExclusionService.ExcludeFromBackup(databasePath.FilePath + "-wal");
        backupExclusionService.ExcludeFromBackup(databasePath.FilePath + "-shm");
    }

    private static async Task SeedDemoContentAsync(IServiceScope scope)
    {
        var demoContentService = scope.ServiceProvider.GetRequiredService<IDemoContentService>();
        await demoContentService.EnsureSeededAsync();
    }

    private static async Task CleanupRetainedDataAsync(IServiceScope scope)
    {
        var cleanupService = scope.ServiceProvider.GetRequiredService<IRetentionCleanupService>();
        await cleanupService.CleanupAsync();
    }

    private static async Task ApplyThemeAsync(IServiceScope scope)
    {
        var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepository.GetAsync();
        var themeService = scope.ServiceProvider.GetRequiredService<IAppThemeService>();
        themeService.ApplyTheme(settings.Theme);
    }

    private static void StartNetworkMonitoring(IServiceScope scope)
    {
        // Den Netzwerkstatus-Service frueh aufloesen, damit das Monitoring startet.
        _ = scope.ServiceProvider.GetRequiredService<INetworkStatusService>();
    }

    private static async Task StartAutoRefreshAsync(IServiceScope scope)
    {
        var autoRefreshService = scope.ServiceProvider.GetRequiredService<IAutoRefreshService>();
        await autoRefreshService.StartAsync();
    }

    // Ein Fehler in einem einzelnen Start-Schritt darf den App-Start nicht verhindern.
    private static async Task RunStartupStepAsync(Func<Task> step, string failureMessage, IDebugLogService debugLogService)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            LogStartupFailure(failureMessage, ex, debugLogService);
        }
    }

    private static void RunStartupStep(Action step, string failureMessage, IDebugLogService debugLogService)
    {
        // Delegiert an den Async-Overload; das Task.CompletedTask-Wrapper ist
        // bereits abgeschlossen, daher blockiert GetResult() nicht.
        RunStartupStepAsync(
            () =>
            {
                step();
                return Task.CompletedTask;
            },
            failureMessage,
            debugLogService).GetAwaiter().GetResult();
    }

    private static void LogStartupFailure(string failureMessage, Exception ex, IDebugLogService debugLogService)
    {
        Debug.WriteLine($"App.OnStart {failureMessage}: {ex}");
        _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, failureMessage, ex.ToString(), DebugLogLevel.Error);
    }
}
