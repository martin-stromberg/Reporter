// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to the content store that holds the
/// re-downloadable article contents (<c>Item.ContentHtml</c>) and article
/// images (<c>Item.Image</c>) separately from the user database. Writes are
/// field-wise upserts: a non-<c>null</c> image writes the image columns while
/// a <c>null</c> image leaves stored image data untouched; a <c>null</c> or
/// empty content removes the stored content only when no image is supplied —
/// with an image the stored content stays untouched. A row is removed only
/// when it would hold neither content nor image after the operation.
/// </summary>
public interface IItemContentStore
{
    /// <summary>
    /// Gets the stored content of the item with the specified identifier asynchronously.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTML content, or <c>null</c> if none is stored.</returns>
    Task<string?> GetAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored contents for the specified item identifiers asynchronously.
    /// </summary>
    /// <param name="itemIds">The item identifiers.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a lookup from item identifier to HTML content; items without stored content are absent.</returns>
    Task<IReadOnlyDictionary<Guid, string>> GetRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored image of the item with the specified identifier asynchronously.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the image, or <c>null</c> if none is stored.</returns>
    Task<ItemImage?> GetImageAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets which of the specified item identifiers have a stored image
    /// asynchronously, without loading the image data itself.
    /// </summary>
    /// <param name="itemIds">The item identifiers to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the subset of <paramref name="itemIds"/> with a stored image.</returns>
    Task<IReadOnlySet<Guid>> GetImageIdsAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the content and optionally the image of the item with the specified
    /// identifier asynchronously. A <c>null</c> or empty content removes the
    /// stored content when <paramref name="image"/> is <c>null</c> and leaves it
    /// untouched otherwise; a <c>null</c> image leaves a stored image untouched.
    /// The row is removed when it would hold neither content nor image afterwards.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="contentHtml">The HTML content to store, or <c>null</c> to remove the stored content (kept when <paramref name="image"/> is supplied).</param>
    /// <param name="image">The article image to store, or <c>null</c> to leave a stored image untouched.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync(Guid itemId, string? contentHtml, ItemImage? image = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the supplied item contents asynchronously in a single batch,
    /// applying the same field-wise upsert rule as <see cref="SetAsync"/> to
    /// each <see cref="ItemContentEntry"/>.
    /// </summary>
    /// <param name="entries">The item contents to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetRangeAsync(IReadOnlyList<ItemContentEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored content of the item with the specified identifier asynchronously.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored contents of the specified item identifiers asynchronously.
    /// </summary>
    /// <param name="itemIds">The item identifiers.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all item identifiers with a stored content or image asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item identifiers.</returns>
    Task<IReadOnlyList<Guid>> GetItemIdsAsync(CancellationToken cancellationToken = default);
}
