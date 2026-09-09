#nullable enable

using System.Globalization;
using System.Resources;

namespace Reporter.Resources.Strings;

/// <summary>
/// Provides strongly typed access to localized RESX resources.
/// </summary>
public static class AppResources
{
    /// <summary>
    /// Gets the cached <see cref="ResourceManager"/> instance for this resource category.
    /// </summary>
    public static ResourceManager ResourceManager { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="CultureInfo"/> used for resource lookups.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets the localized welcome text.
    /// </summary>
    public static string Welcome { get; private set; } = null!;

    /// <summary>
    /// Gets the localized title for the unread page.
    /// </summary>
    public static string PageTitleUnread { get; private set; } = null!;

    /// <summary>
    /// Gets the localized title for the feeds page.
    /// </summary>
    public static string PageTitleFeeds { get; private set; } = null!;

    /// <summary>
    /// Gets the localized title for the later page.
    /// </summary>
    public static string PageTitleLater { get; private set; } = null!;

    /// <summary>
    /// Gets the localized title for the settings page.
    /// </summary>
    public static string PageTitleSettings { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the unread tab.
    /// </summary>
    public static string TabUnread { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the feeds tab.
    /// </summary>
    public static string TabFeeds { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the later tab.
    /// </summary>
    public static string TabLater { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the settings tab.
    /// </summary>
    public static string TabSettings { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the unread page.
    /// </summary>
    public static string PlaceholderUnread { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the feeds page.
    /// </summary>
    public static string PlaceholderFeeds { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the later page.
    /// </summary>
    public static string PlaceholderLater { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the settings page.
    /// </summary>
    public static string PlaceholderSettings { get; private set; } = null!;

    static AppResources()
    {
        ResourceManager = new ResourceManager("Reporter.Resources.Strings.AppResources", typeof(AppResources).Assembly);

        Welcome = GetString(nameof(Welcome));
        PageTitleUnread = GetString(nameof(PageTitleUnread));
        PageTitleFeeds = GetString(nameof(PageTitleFeeds));
        PageTitleLater = GetString(nameof(PageTitleLater));
        PageTitleSettings = GetString(nameof(PageTitleSettings));
        TabUnread = GetString(nameof(TabUnread));
        TabFeeds = GetString(nameof(TabFeeds));
        TabLater = GetString(nameof(TabLater));
        TabSettings = GetString(nameof(TabSettings));
        PlaceholderUnread = GetString(nameof(PlaceholderUnread));
        PlaceholderFeeds = GetString(nameof(PlaceholderFeeds));
        PlaceholderLater = GetString(nameof(PlaceholderLater));
        PlaceholderSettings = GetString(nameof(PlaceholderSettings));
    }

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, Culture) ?? name;
    }
}
