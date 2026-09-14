// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed article/item in the domain model.
/// </summary>
public class Item
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
    /// Gets the original GUID or hash of the item.
    /// </summary>
    public required string GuidOrHash { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item has been read.
    /// </summary>
    public required bool IsRead { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is saved for later.
    /// </summary>
    public required bool IsSavedForLater { get; init; }

    /// <summary>
    /// Gets the timestamp when the item was read.
    /// </summary>
    public DateTime? ReadAt { get; init; }

    /// <summary>
    /// Gets the HTML content for offline reading.
    /// </summary>
    public string? ContentHtml { get; init; }
}
