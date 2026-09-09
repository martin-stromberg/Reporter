#nullable enable

using System.Globalization;
using System.Resources;

namespace Reporter.Resources.Strings;

public static class AppResources
{
    private static ResourceManager? _resourceManager;
    private static CultureInfo? _resourceCulture;

    public static ResourceManager ResourceManager
    {
        get
        {
            if (_resourceManager is null)
            {
                _resourceManager = new ResourceManager("Reporter.Resources.Strings.AppResources", typeof(AppResources).Assembly);
            }

            return _resourceManager;
        }
    }

    public static CultureInfo? Culture
    {
        get => _resourceCulture;
        set => _resourceCulture = value;
    }

    public static string Welcome => GetString(nameof(Welcome));

    public static string PageTitleUnread => GetString(nameof(PageTitleUnread));

    public static string PageTitleFeeds => GetString(nameof(PageTitleFeeds));

    public static string PageTitleLater => GetString(nameof(PageTitleLater));

    public static string PageTitleSettings => GetString(nameof(PageTitleSettings));

    public static string TabUnread => GetString(nameof(TabUnread));

    public static string TabFeeds => GetString(nameof(TabFeeds));

    public static string TabLater => GetString(nameof(TabLater));

    public static string TabSettings => GetString(nameof(TabSettings));

    public static string PlaceholderUnread => GetString(nameof(PlaceholderUnread));

    public static string PlaceholderFeeds => GetString(nameof(PlaceholderFeeds));

    public static string PlaceholderLater => GetString(nameof(PlaceholderLater));

    public static string PlaceholderSettings => GetString(nameof(PlaceholderSettings));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, _resourceCulture) ?? name;
    }
}
