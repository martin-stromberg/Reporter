namespace Reporter.Data.Entities;

/// <summary>
/// Represents the singleton application settings record.
/// </summary>
public class Settings
{
    /// <summary>
    /// Gets the default singleton identifier.
    /// </summary>
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
    public string? AutoMarkReadMode { get; set; } = "on_scroll";

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
}
