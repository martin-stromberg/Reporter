// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Data.Entities;

/// <summary>
/// Represents the singleton application settings record.
/// </summary>
public class Settings
{
    /// <summary>
    /// Gets the default singleton identifier.
    /// </summary>
    /// <value>The default singleton identifier.</value>
    public static readonly Guid DefaultId = new("a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a");

    /// <summary>
    /// Gets or sets the unique identifier of the settings record.
    /// </summary>
    public Guid Id { get; set; } = DefaultId;

    /// <summary>
    /// Gets or sets the retention period for items in days.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the auto-mark-as-read mode.
    /// </summary>
    public string? AutoMarkReadMode { get; set; } = SettingsValues.AutoMarkReadOnScroll;

    /// <summary>
    /// Gets or sets the delay before an item is marked as read automatically.
    /// </summary>
    public int AutoMarkReadDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the start of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursStart { get; set; }

    /// <summary>
    /// Gets or sets the end of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic background refresh is enabled.
    /// </summary>
    public bool AutoRefreshEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the background refresh interval in minutes.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether feeds are refreshed when the application starts.
    /// </summary>
    public bool RefreshOnStartupEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sort order of the unread articles list ("desc" or "asc").
    /// </summary>
    public string? UnreadSortOrder { get; set; } = SettingsValues.SortOrderDescending;

    /// <summary>
    /// Gets or sets the appearance theme ("system", "light" or "dark").
    /// </summary>
    public string? Theme { get; set; } = SettingsValues.ThemeSystem;

    /// <summary>
    /// Gets or sets a value indicating whether new items trigger a single summary
    /// notification per feed instead of one notification per item.
    /// </summary>
    public bool NotificationSummaryEnabled { get; set; }

    /// <summary>
    /// Gets or sets the language selection ("system", "de" or "en").
    /// </summary>
    public string? Language { get; set; } = SettingsValues.LanguageSystem;
}
