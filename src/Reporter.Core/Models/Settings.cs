namespace Reporter.Core.Models;

/// <summary>
/// Represents the singleton application settings in the domain model.
/// </summary>
public class Settings
{
    /// <summary>
    /// Gets the default singleton identifier.
    /// </summary>
    /// <value>The default singleton identifier.</value>
    public static readonly Guid DefaultId = new("a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a");

    /// <summary>
    /// Gets the unique identifier of the settings record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the retention period for items in days.
    /// </summary>
    public required int RetentionDays { get; init; }

    /// <summary>
    /// Gets the auto-mark-as-read mode.
    /// </summary>
    public string? AutoMarkReadMode { get; init; }

    /// <summary>
    /// Gets the delay before an item is marked as read automatically.
    /// </summary>
    public required int AutoMarkReadDelaySeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether notifications are enabled.
    /// </summary>
    public required bool NotificationsEnabled { get; init; }

    /// <summary>
    /// Gets the start of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursStart { get; init; }

    /// <summary>
    /// Gets the end of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursEnd { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic background refresh is enabled.
    /// </summary>
    public required bool AutoRefreshEnabled { get; init; }

    /// <summary>
    /// Gets the background refresh interval in minutes.
    /// </summary>
    public required int RefreshIntervalMinutes { get; init; }

    /// <summary>
    /// Gets the appearance theme ("system", "light" or "dark").
    /// </summary>
    public string? Theme { get; init; } = SettingsValues.ThemeSystem;

    /// <summary>
    /// Gets a value indicating whether new items trigger a single summary notification
    /// per feed (<c>true</c>) or one notification per item (<c>false</c>, the default).
    /// </summary>
    public required bool NotificationSummaryEnabled { get; init; }
}
