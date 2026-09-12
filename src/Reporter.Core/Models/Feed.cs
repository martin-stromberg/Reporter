// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents an RSS/Atom feed source in the domain model.
/// </summary>
public class Feed
{
    /// <summary>
    /// Gets the unique identifier of the feed.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the URL of the feed.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the title of the feed.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the optional category identifier the feed belongs to.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the timestamp of the last feed check.
    /// </summary>
    public DateTime? LastCheckedAt { get; init; }

    /// <summary>
    /// Gets the current health status of the feed.
    /// </summary>
    public string? HealthStatus { get; init; }

    /// <summary>
    /// Gets the timestamp of the last health status change.
    /// </summary>
    public DateTime? HealthLastChange { get; init; }

    /// <summary>
    /// Gets a value indicating whether notifications are enabled for this feed.
    /// </summary>
    public required bool NotificationsEnabled { get; init; }
}
