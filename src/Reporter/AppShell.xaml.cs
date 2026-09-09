using Microsoft.Extensions.DependencyInjection;
using Reporter.Views;

namespace Reporter;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        var unreadTab = new Tab { Title = "Ungelesen" };
        unreadTab.Items.Add(new ShellContent { Title = "Ungelesen", Content = services.GetRequiredService<UnreadPage>() });

        var feedsTab = new Tab { Title = "Feeds" };
        feedsTab.Items.Add(new ShellContent { Title = "Feeds", Content = services.GetRequiredService<FeedsPage>() });

        var laterTab = new Tab { Title = "Später" };
        laterTab.Items.Add(new ShellContent { Title = "Später", Content = services.GetRequiredService<LaterPage>() });

        var settingsTab = new Tab { Title = "Einstellungen" };
        settingsTab.Items.Add(new ShellContent { Title = "Einstellungen", Content = services.GetRequiredService<SettingsPage>() });

        var tabBar = new TabBar();
        tabBar.Items.Add(unreadTab);
        tabBar.Items.Add(feedsTab);
        tabBar.Items.Add(laterTab);
        tabBar.Items.Add(settingsTab);

        Items.Add(tabBar);
    }
}
