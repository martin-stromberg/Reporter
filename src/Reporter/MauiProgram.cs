// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reporter.Core.Interfaces;
using Reporter.Core.Localization;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data;
using Reporter.Data.Repositories;
using Reporter.Services;
using Reporter.Views;

namespace Reporter;

/// <summary>
/// Configures and builds the .NET MAUI application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the <see cref="MauiApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Newsreader-Regular.ttf", "NewsreaderRegular");
                fonts.AddFont("Newsreader-Medium.ttf", "NewsreaderMedium");
                fonts.AddFont("Newsreader-SemiBold.ttf", "NewsreaderSemiBold");
                fonts.AddFont("Newsreader-Italic.ttf", "NewsreaderItalic");
            });

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "reporter.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        builder.Services
            .AddDbContextFactory<ReporterDbContext>(options => options.UseSqlite($"Data Source={databasePath}"))
            .AddSingleton<IFeedRepository, FeedRepository>()
            .AddSingleton<ICategoryRepository, CategoryRepository>()
            .AddSingleton<IItemRepository, ItemRepository>()
            .AddSingleton<IKeywordRepository, KeywordRepository>()
            .AddSingleton<ISettingsRepository, SettingsRepository>()
            .AddSingleton<ISyncLogRepository, SyncLogRepository>()
            .AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            .AddSingleton<IFeedSearchService, FeedSearchService>()
            .AddSingleton<IFeedSyncService, FeedSyncService>()
            .AddSingleton<IRetentionCleanupService, RetentionCleanupService>()
            .AddSingleton<IKeywordMatcher, KeywordMatcher>()
            .AddSingleton<IKeywordFilter, KeywordFilter>()
            .AddSingleton<IAutoRefreshService, AutoRefreshService>()
            .AddSingleton<IAppThemeService, AppThemeService>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<ILocalNotificationService, LocalNotificationService>()
            .AddSingleton<INetworkStatusService, NetworkStatusService>()
            .AddSingleton<UnreadViewModel>()
            .AddSingleton<FeedsViewModel>()
            .AddSingleton<LaterViewModel>()
            .AddSingleton<CategoriesViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddTransient<ArticleDetailViewModel>()
            .AddTransient<ArticleDetailPage>()
            .AddTransient<UnreadPage>()
            .AddTransient<FeedsPage>()
            .AddTransient<LaterPage>()
            .AddTransient<CategoriesPage>()
            .AddTransient<SettingsPage>()
            .AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ApplyPersistedLanguage(app);
        return app;
    }

    /// <summary>
    /// Applies the persisted language selection to the process-wide culture before
    /// <see cref="App.CreateWindow"/> resolves the shell: <c>AppShell</c> reads the
    /// localized tab titles and creates all pages (including the singleton
    /// <see cref="SettingsViewModel"/>, which reads its option labels from
    /// <c>AppResources</c>) eagerly, so the culture must already be effective here.
    /// The database is migrated synchronously first so the <c>language</c> column
    /// exists on upgraded databases.
    /// </summary>
    /// <param name="app">The built <see cref="MauiApp"/> whose services are used.</param>
    private static void ApplyPersistedLanguage(MauiApp app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReporterDbContext>();
            context.Database.Migrate();

            var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var settings = settingsRepository.GetAsync().GetAwaiter().GetResult();
            AppCulture.Apply(settings.Language);
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Anwenden der Sprache darf den App-Start nicht verhindern.
            Debug.WriteLine($"MauiProgram.ApplyPersistedLanguage failed: {ex}");
        }
    }
}
