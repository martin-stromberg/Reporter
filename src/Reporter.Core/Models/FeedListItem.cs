namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed together with display information such as category name and unread count.
/// </summary>
public class FeedListItem
{
    /// <summary>
    /// Gets the unique identifier of the feed.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the display title of the feed.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the URL of the feed.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the optional category identifier the feed belongs to.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the display name of the category, or <c>null</c> if no category is assigned.
    /// </summary>
    public string? CategoryName { get; init; }

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
    /// Gets the number of unread items for this feed.
    /// </summary>
    public required int UnreadCount { get; init; }
}
