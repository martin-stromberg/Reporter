using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Data;

namespace Reporter;

/// <summary>
/// Represents the cross-platform .NET MAUI application.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;

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

        try
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<IRetentionCleanupService>();
            await cleanupService.CleanupAsync();
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Aufraeumen darf den App-Start nicht verhindern.
            Debug.WriteLine($"App.OnStart retention cleanup failed: {ex}");
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
        }
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
}
