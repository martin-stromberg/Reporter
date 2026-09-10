using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Resources.Strings;
using Reporter.Views;

namespace Reporter;

/// <summary>
/// Defines the shell-based navigation structure of the application.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    /// <param name="services">The application's service provider used to resolve pages.</param>
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        Routing.RegisterRoute("articledetail", typeof(ArticleDetailPage));

        var unreadTab = new Tab { Title = AppResources.TabUnread };
        unreadTab.Items.Add(new ShellContent { Title = AppResources.TabUnread, Content = services.GetRequiredService<UnreadPage>() });

        var feedsTab = new Tab { Title = AppResources.TabFeeds };
        feedsTab.Items.Add(new ShellContent { Title = AppResources.TabFeeds, Content = services.GetRequiredService<FeedsPage>() });

        var laterTab = new Tab { Title = AppResources.TabLater };
        laterTab.Items.Add(new ShellContent { Title = AppResources.TabLater, Content = services.GetRequiredService<LaterPage>() });

        var categoriesTab = new Tab { Title = AppResources.TabCategories };
        categoriesTab.Items.Add(new ShellContent { Title = AppResources.TabCategories, Content = services.GetRequiredService<CategoriesPage>() });

        var settingsTab = new Tab { Title = AppResources.TabSettings };
        settingsTab.Items.Add(new ShellContent { Title = AppResources.TabSettings, Content = services.GetRequiredService<SettingsPage>() });

        var tabBar = new TabBar();
        tabBar.Items.Add(unreadTab);
        tabBar.Items.Add(feedsTab);
        tabBar.Items.Add(laterTab);
        tabBar.Items.Add(categoriesTab);
        tabBar.Items.Add(settingsTab);

        Items.Add(tabBar);
    }
}
