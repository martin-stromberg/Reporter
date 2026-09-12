// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Data.Entities;

/// <summary>
/// Represents an RSS/Atom feed source.
/// </summary>
public class Feed
{
    /// <summary>
    /// Gets or sets the unique identifier of the feed.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the feed URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feed title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional category identifier.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last feed check.
    /// </summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// Gets or sets the current health status of the feed.
    /// </summary>
    public string? HealthStatus { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last health status change.
    /// </summary>
    public DateTime? HealthLastChange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled for this feed.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional category of the feed.
    /// </summary>
    public Category? Category { get; set; }
}
