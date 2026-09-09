using Microsoft.Extensions.DependencyInjection;
using Reporter.Resources.Strings;
using Reporter.Views;

namespace Reporter;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        var unreadTab = new Tab { Title = AppResources.TabUnread };
        unreadTab.Items.Add(new ShellContent { Title = AppResources.TabUnread, Content = services.GetRequiredService<UnreadPage>() });

        var feedsTab = new Tab { Title = AppResources.TabFeeds };
        feedsTab.Items.Add(new ShellContent { Title = AppResources.TabFeeds, Content = services.GetRequiredService<FeedsPage>() });

        var laterTab = new Tab { Title = AppResources.TabLater };
        laterTab.Items.Add(new ShellContent { Title = AppResources.TabLater, Content = services.GetRequiredService<LaterPage>() });

        var settingsTab = new Tab { Title = AppResources.TabSettings };
        settingsTab.Items.Add(new ShellContent { Title = AppResources.TabSettings, Content = services.GetRequiredService<SettingsPage>() });

        var tabBar = new TabBar();
        tabBar.Items.Add(unreadTab);
        tabBar.Items.Add(feedsTab);
        tabBar.Items.Add(laterTab);
        tabBar.Items.Add(settingsTab);

        Items.Add(tabBar);
    }
}
