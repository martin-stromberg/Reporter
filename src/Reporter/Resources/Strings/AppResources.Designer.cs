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

    /// <summary>
    /// Gets the localized title for the categories page.
    /// </summary>
    public static string PageTitleCategories { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the categories tab.
    /// </summary>
    public static string TabCategories { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the categories page.
    /// </summary>
    public static string PlaceholderCategories { get; private set; } = null!;

    /// <summary>
    /// Gets the localized header for the category name column.
    /// </summary>
    public static string LabelCategoryName { get; private set; } = null!;

    /// <summary>
    /// Gets the localized header for the feed count column.
    /// </summary>
    public static string LabelCategoryFeedCount { get; private set; } = null!;

    /// <summary>
    /// Gets the localized save button text.
    /// </summary>
    public static string ButtonSave { get; private set; } = null!;

    /// <summary>
    /// Gets the localized edit button text.
    /// </summary>
    public static string ButtonEdit { get; private set; } = null!;

    /// <summary>
    /// Gets the localized delete button text.
    /// </summary>
    public static string ButtonDelete { get; private set; } = null!;

    /// <summary>
    /// Gets the localized no button text.
    /// </summary>
    public static string ButtonNo { get; private set; } = null!;

    /// <summary>
    /// Gets the localized yes button text.
    /// </summary>
    public static string ButtonYes { get; private set; } = null!;

    /// <summary>
    /// Gets the localized text for no category.
    /// </summary>
    public static string CategoryNone { get; private set; } = null!;

    /// <summary>
    /// Gets the localized delete confirmation message for a feed.
    /// </summary>
    public static string ConfirmDeleteFeedMessage { get; private set; } = null!;

    /// <summary>
    /// Gets the localized delete confirmation title for a feed.
    /// </summary>
    public static string ConfirmDeleteFeedTitle { get; private set; } = null!;

    /// <summary>
    /// Gets the localized error for a duplicate feed URL.
    /// </summary>
    public static string ErrorFeedDuplicate { get; private set; } = null!;

    /// <summary>
    /// Gets the localized error for a missing feed title.
    /// </summary>
    public static string ErrorFeedTitleEmpty { get; private set; } = null!;

    /// <summary>
    /// Gets the localized error for an invalid feed URL.
    /// </summary>
    public static string ErrorFeedUrlInvalid { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the feed category.
    /// </summary>
    public static string LabelFeedCategory { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the feed health status.
    /// </summary>
    public static string LabelFeedHealthStatus { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the feed last check timestamp.
    /// </summary>
    public static string LabelFeedLastCheck { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the feed title.
    /// </summary>
    public static string LabelFeedTitle { get; private set; } = null!;

    /// <summary>
    /// Gets the localized label for the unread feed item count.
    /// </summary>
    public static string LabelFeedUnreadCount { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the feed title.
    /// </summary>
    public static string PlaceholderFeedTitle { get; private set; } = null!;

    /// <summary>
    /// Gets the localized placeholder text for the feed URL.
    /// </summary>
    public static string PlaceholderFeedUrl { get; private set; } = null!;

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
        PageTitleCategories = GetString(nameof(PageTitleCategories));
        TabCategories = GetString(nameof(TabCategories));
        PlaceholderCategories = GetString(nameof(PlaceholderCategories));
        LabelCategoryName = GetString(nameof(LabelCategoryName));
        LabelCategoryFeedCount = GetString(nameof(LabelCategoryFeedCount));
        ButtonSave = GetString(nameof(ButtonSave));
        ButtonEdit = GetString(nameof(ButtonEdit));
        ButtonDelete = GetString(nameof(ButtonDelete));
        ButtonNo = GetString(nameof(ButtonNo));
        ButtonYes = GetString(nameof(ButtonYes));
        CategoryNone = GetString(nameof(CategoryNone));
        ConfirmDeleteFeedMessage = GetString(nameof(ConfirmDeleteFeedMessage));
        ConfirmDeleteFeedTitle = GetString(nameof(ConfirmDeleteFeedTitle));
        ErrorFeedDuplicate = GetString(nameof(ErrorFeedDuplicate));
        ErrorFeedTitleEmpty = GetString(nameof(ErrorFeedTitleEmpty));
        ErrorFeedUrlInvalid = GetString(nameof(ErrorFeedUrlInvalid));
        LabelFeedCategory = GetString(nameof(LabelFeedCategory));
        LabelFeedHealthStatus = GetString(nameof(LabelFeedHealthStatus));
        LabelFeedLastCheck = GetString(nameof(LabelFeedLastCheck));
        LabelFeedTitle = GetString(nameof(LabelFeedTitle));
        LabelFeedUnreadCount = GetString(nameof(LabelFeedUnreadCount));
        PlaceholderFeedTitle = GetString(nameof(PlaceholderFeedTitle));
        PlaceholderFeedUrl = GetString(nameof(PlaceholderFeedUrl));
    }

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, Culture) ?? name;
    }
}
