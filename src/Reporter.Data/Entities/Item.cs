namespace Reporter.Data.Entities;

/// <summary>
/// Represents a feed article/item.
/// </summary>
public class Item
{
    /// <summary>
    /// Gets or sets the unique identifier of the item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the feed identifier the item belongs to.
    /// </summary>
    public Guid FeedId { get; set; }

    /// <summary>
    /// Gets or sets the item title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item link.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the publication timestamp.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the original GUID or hash of the item.
    /// </summary>
    public string GuidOrHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the item has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is saved for later.
    /// </summary>
    public bool IsSavedForLater { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the item was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Gets or sets the HTML content for offline reading.
    /// </summary>
    public string? ContentHtml { get; set; }

    /// <summary>
    /// Gets or sets the feed the item belongs to.
    /// </summary>
    public Feed Feed { get; set; } = null!;
}
