// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
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
        var context = scope.ServiceProvider.GetRequiredService<ReporterDbContext>();
        await context.Database.MigrateAsync();

        var debugLogService = scope.ServiceProvider.GetRequiredService<IDebugLogService>();
        _debugLogService = debugLogService;
        await debugLogService.BeginSessionAsync();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            var demoContentService = scope.ServiceProvider.GetRequiredService<IDemoContentService>();
            await demoContentService.EnsureSeededAsync();
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Seeden des Demo-Inhalts darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart demo content seed failed: {ex}");
            _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Demo content seed failed", ex.ToString(), DebugLogLevel.Error);
        }

        try
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IRetentionCleanupService>();
            await cleanupService.CleanupAsync();
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Aufraeumen darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart retention cleanup failed: {ex}");
            _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Retention cleanup failed on start", ex.ToString(), DebugLogLevel.Error);
        }

        try
        {
            var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var settings = await settingsRepository.GetAsync();
            var themeService = scope.ServiceProvider.GetRequiredService<IAppThemeService>();
            themeService.ApplyTheme(settings.Theme);
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Anwenden des Themes darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart theme apply failed: {ex}");
            _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Theme apply failed on start", ex.ToString(), DebugLogLevel.Error);
        }

        try
        {
            // Den Netzwerkstatus-Service frueh aufloesen, damit das Monitoring startet.
            _ = scope.ServiceProvider.GetRequiredService<INetworkStatusService>();
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Starten der Netzwerk-Ueberwachung darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart network status init failed: {ex}");
            _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Network status init failed on start", ex.ToString(), DebugLogLevel.Error);
        }

        try
        {
            var autoRefreshService = scope.ServiceProvider.GetRequiredService<IAutoRefreshService>();
            await autoRefreshService.StartAsync();
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Starten der Hintergrund-Aktualisierung darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart auto refresh start failed: {ex}");
            _ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Auto refresh start failed", ex.ToString(), DebugLogLevel.Error);
        }
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
}
