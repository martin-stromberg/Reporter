// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Data.Entities;

/// <summary>
/// Represents the re-downloadable HTML content of a feed item, stored in the
/// separate content database (<c>reporter-content.db</c>) so the article bulk
/// data stays excluded from the cloud backup while the item row itself is
/// included.
/// </summary>
public class ItemContent
{
    /// <summary>
    /// Gets or sets the identifier of the item the content belongs to.
    /// Matches <c>items.id</c> in the main database; no cross-database
    /// foreign key exists.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the HTML content for offline reading.
    /// </summary>
    public string? ContentHtml { get; set; }

    /// <summary>
    /// Gets or sets the binary data of the locally stored article image.
    /// </summary>
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the stored article image.
    /// </summary>
    public string? ImageContentType { get; set; }

    /// <summary>
    /// Gets or sets the origin URL the article image was downloaded from.
    /// </summary>
    public string? ImageUrl { get; set; }
}
