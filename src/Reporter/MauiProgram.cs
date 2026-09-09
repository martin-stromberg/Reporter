using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reporter.Core.Interfaces;
using Reporter.Core.Services;
using Reporter.Data.Repositories;
using Reporter.ViewModels;
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

        builder.Services
            .AddSingleton<IArticleRepository, ArticleRepository>()
            .AddSingleton<IArticleService, ArticleService>()
            .AddSingleton<UnreadViewModel>()
            .AddSingleton<FeedsViewModel>()
            .AddSingleton<LaterViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddTransient<UnreadPage>()
            .AddTransient<FeedsPage>()
            .AddTransient<LaterPage>()
            .AddTransient<SettingsPage>()
            .AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
