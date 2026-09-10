namespace Reporter.Core.Models;

/// <summary>
/// Represents an article for display on the unread dashboard.
/// </summary>
public class ItemListItem
{
    /// <summary>
    /// Gets the unique identifier of the item.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the feed identifier the item belongs to.
    /// </summary>
    public required Guid FeedId { get; init; }

    /// <summary>
    /// Gets the title of the item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the link to the original article.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Gets the publication timestamp of the item.
    /// </summary>
    public DateTime? PublishedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item has been read.
    /// </summary>
    public required bool IsRead { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is saved for later.
    /// </summary>
    public required bool IsSavedForLater { get; init; }

    /// <summary>
    /// Gets the display title of the feed the item belongs to.
    /// </summary>
    public string FeedTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional category identifier of the feed.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the display name of the category, or <c>null</c> if no category is assigned.
    /// </summary>
    public string? CategoryName { get; init; }
}
