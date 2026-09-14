// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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

    /// <summary>
    /// Gets the optional image URL extracted from the article content.
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Gets the favicon URL of the item's feed, or <c>null</c> when none was discovered.
    /// </summary>
    public string? FeedFaviconUrl { get; init; }

    /// <summary>
    /// Gets the first letter of the feed title in upper case for the fallback avatar,
    /// or <c>"?"</c> when the feed title is empty.
    /// </summary>
    public string FeedInitial
    {
        get
        {
            return FeedAvatar.Initial(FeedTitle);
        }
    }

    /// <summary>
    /// Gets a plain-text summary of the article content.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets the formatted reading time estimate, or <c>null</c> when unavailable.
    /// </summary>
    public string? ReadingTimeText { get; init; }

    /// <summary>
    /// Creates a copy of this item with optionally overridden read or saved-for-later state.
    /// </summary>
    /// <param name="isRead">The new read state, or <c>null</c> to keep the current value.</param>
    /// <param name="isSavedForLater">The new saved-for-later state, or <c>null</c> to keep the current value.</param>
    /// <returns>A new <see cref="ItemListItem"/> with the requested state applied.</returns>
    public ItemListItem CopyWith(bool? isRead = null, bool? isSavedForLater = null)
    {
        return new ItemListItem
        {
            Id = Id,
            FeedId = FeedId,
            Title = Title,
            Link = Link,
            PublishedAt = PublishedAt,
            IsRead = isRead ?? IsRead,
            IsSavedForLater = isSavedForLater ?? IsSavedForLater,
            FeedTitle = FeedTitle,
            CategoryId = CategoryId,
            CategoryName = CategoryName,
            ImageUrl = ImageUrl,
            FeedFaviconUrl = FeedFaviconUrl,
            Summary = Summary,
            ReadingTimeText = ReadingTimeText,
        };
    }
}
