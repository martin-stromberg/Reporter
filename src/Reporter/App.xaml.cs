using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    }

    /// <summary>
    /// Creates the application's main window.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>The main application window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_services.GetRequiredService<AppShell>());
    }
}
