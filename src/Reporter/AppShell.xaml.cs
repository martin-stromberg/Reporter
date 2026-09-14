// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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

        var unreadTab = new Tab { Title = AppResources.TabUnread, Icon = "tab_unread.png" };
        unreadTab.Items.Add(new ShellContent { Route = "unread", Title = AppResources.TabUnread, Content = services.GetRequiredService<UnreadPage>() });

        var feedsTab = new Tab { Title = AppResources.TabFeeds, Icon = "tab_feeds.png" };
        feedsTab.Items.Add(new ShellContent { Title = AppResources.TabFeeds, Content = services.GetRequiredService<FeedsPage>() });

        var laterTab = new Tab { Title = AppResources.TabLater, Icon = "tab_later.png" };
        laterTab.Items.Add(new ShellContent { Title = AppResources.TabLater, Content = services.GetRequiredService<LaterPage>() });

        var categoriesTab = new Tab { Title = AppResources.TabCategories, Icon = "tab_categories.png" };
        categoriesTab.Items.Add(new ShellContent { Title = AppResources.TabCategories, Content = services.GetRequiredService<CategoriesPage>() });

        var settingsTab = new Tab { Title = AppResources.TabSettings, Icon = "tab_settings.png" };
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
