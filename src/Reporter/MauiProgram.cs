// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reporter.Core.Interfaces;
using Reporter.Core.Localization;
using Reporter.Core.Models;
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

        // REPORTER_DB_PATH allows the E2E suite to point the app at an isolated
        // database file so test runs never touch the developer's real data.
        var databasePathOverride = Environment.GetEnvironmentVariable("REPORTER_DB_PATH");
        var databasePath = string.IsNullOrWhiteSpace(databasePathOverride)
            ? Path.Combine(FileSystem.AppDataDirectory, "reporter.db")
            : databasePathOverride;
        var databaseDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        // The database file only exists after a previous run created it; its
        // absence marks the very first start (or a fresh database after a
        // deletion) and gates the one-time demo content seed. It must be
        // evaluated before builder.Build(), because ApplyPersistedLanguage
        // migrates — and thereby creates — the database file.
        var isFirstRun = !File.Exists(databasePath);

        // Die Content-Datenbank liegt neben reporter.db im selben Verzeichnis;
        // ein REPORTER_DB_PATH-Override verlagert damit beide Dateien (E2E-Hermetik).
        var contentDatabasePath = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(databasePath))!,
            "reporter-content.db");

        // REPORTER_FEEDSEARCH_ENDPOINT points the feed search at a stub server
        // during E2E runs; invalid values fall back to the built-in default.
        var feedSearchEndpoint = ResolveFeedSearchEndpoint(Environment.GetEnvironmentVariable("REPORTER_FEEDSEARCH_ENDPOINT"));

        // REPORTER_DISABLE_DEMO_SEED keeps the demo content seed out of E2E
        // and CI runs; any set value other than "0"/"false" suppresses it.
        var demoSeedSuppressed = ResolveDemoSeedSuppressed(
            Environment.GetEnvironmentVariable("REPORTER_DISABLE_DEMO_SEED"));

        builder.Services
            .AddDbContextFactory<ReporterDbContext>(options => options.UseSqlite($"Data Source={databasePath}"))
            .AddDbContextFactory<ContentDbContext>(options => options.UseSqlite($"Data Source={contentDatabasePath}"))
            .AddSingleton<IFeedRepository, FeedRepository>()
            .AddSingleton<ICategoryRepository, CategoryRepository>()
            .AddSingleton<IItemRepository, ItemRepository>()
            .AddSingleton<IItemContentStore, ItemContentRepository>()
            .AddSingleton<IContentMigrationService, ItemContentMigrationService>()
            .AddSingleton<IKeywordRepository, KeywordRepository>()
            .AddSingleton<ISettingsRepository, SettingsRepository>()
            .AddSingleton<ISyncLogRepository, SyncLogRepository>()
            .AddSingleton<IDebugLogRepository, DebugLogRepository>()
            .AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            .AddSingleton<IFeedSearchService>(provider =>
                new FeedSearchService(provider.GetRequiredService<HttpClient>(), feedSearchEndpoint))
            .AddSingleton<IFeedIconService, FeedIconService>()
            .AddSingleton<IItemImageService, ItemImageService>()
            .AddSingleton<IFeedSyncService, FeedSyncService>()
            .AddSingleton<IRetentionCleanupService, RetentionCleanupService>()
            .AddSingleton(new FirstRunState { IsFirstRun = isFirstRun, DemoSeedSuppressed = demoSeedSuppressed })
            .AddSingleton(new DatabasePath(databasePath))
            .AddSingleton(new ContentDatabasePath(contentDatabasePath))
            .AddSingleton<IDemoContentService, DemoContentService>()
            .AddSingleton<IKeywordMatcher, KeywordMatcher>()
            .AddSingleton<IKeywordFilter, KeywordFilter>()
            .AddSingleton<IAutoRefreshService, AutoRefreshService>()
            .AddSingleton<IAppThemeService, AppThemeService>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<ILocalNotificationService, LocalNotificationService>()
            .AddSingleton<INetworkStatusService, NetworkStatusService>()
            .AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>()
            .AddSingleton<IBackupExclusionService, BackupExclusionService>()
            .AddSingleton<IScheduledSyncRunner, ScheduledSyncRunner>()
            .AddSingleton<IDebugLogService, DebugLogService>()
            .AddSingleton<IEmailService, EmailService>()
            .AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>()
            .AddSingleton<IDebugReportService, DebugReportService>()
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
        MigrateContentStoreAndLegacyData(app);
        ApplyPersistedLanguage(app);
        return app;
    }

    /// <summary>
    /// Validates the <c>REPORTER_FEEDSEARCH_ENDPOINT</c> override: only absolute
    /// http/https URIs are accepted, anything else falls back to the default endpoint.
    /// </summary>
    /// <param name="value">The raw environment variable value.</param>
    /// <returns>The validated endpoint, or <see langword="null"/> when absent or invalid.</returns>
    private static string? ResolveFeedSearchEndpoint(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.AbsoluteUri
                : null;
    }

    /// <summary>
    /// Evaluates the <c>REPORTER_DISABLE_DEMO_SEED</c> override: any set value
    /// other than <c>"0"</c>/<c>"false"</c> suppresses the demo content seed.
    /// </summary>
    /// <param name="value">The raw environment variable value.</param>
    /// <returns><see langword="true"/> when the demo seed is suppressed; otherwise <see langword="false"/>.</returns>
    private static bool ResolveDemoSeedSuppressed(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Migrates the content database schema and copies legacy
    /// <c>items.content_html</c> values into the content store. Must run before
    /// <see cref="ApplyPersistedLanguage"/>, which migrates <c>reporter.db</c>
    /// and thereby drops the legacy column. A failure is logged but never
    /// blocks the start; <c>App.OnStart</c> retries the content migration as
    /// its own startup step.
    /// </summary>
    /// <param name="app">The built <see cref="MauiApp"/> whose services are used.</param>
    private static void MigrateContentStoreAndLegacyData(MauiApp app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<IContentMigrationService>();
            migrationService.MigrateLegacyContentAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Ein Fehler bei der Content-Migration darf den App-Start nicht verhindern.
            Debug.WriteLine($"MauiProgram.MigrateContentStoreAndLegacyData failed: {ex}");
        }
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
