using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reporter.Core.Interfaces;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data;
using Reporter.Data.Repositories;
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
            .AddSingleton<IFeedSyncService, FeedSyncService>()
            .AddSingleton<UnreadViewModel>()
            .AddSingleton<FeedsViewModel>()
            .AddSingleton<LaterViewModel>()
            .AddSingleton<CategoriesViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddTransient<UnreadPage>()
            .AddTransient<FeedsPage>()
            .AddTransient<LaterPage>()
            .AddTransient<CategoriesPage>()
            .AddTransient<SettingsPage>()
            .AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
