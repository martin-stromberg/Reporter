using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reporter.Core.Interfaces;
using Reporter.Core.Services;
using Reporter.Data.Repositories;
using Reporter.ViewModels;

namespace Reporter;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddSingleton<IArticleRepository, ArticleRepository>()
            .AddSingleton<IArticleService, ArticleService>()
            .AddSingleton<UngelesenViewModel>()
            .AddSingleton<FeedsViewModel>()
            .AddSingleton<SpaeterViewModel>()
            .AddSingleton<EinstellungenViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
