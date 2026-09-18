// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to the content store that holds the
/// re-downloadable article contents (<c>Item.ContentHtml</c>) separately from
/// the user database. Only non-empty contents are stored; writing
/// <c>null</c> or an empty content removes a stored entry (upsert semantics).
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
    /// Stores the content of the item with the specified identifier asynchronously.
    /// A <c>null</c> or empty content removes a stored entry.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="contentHtml">The HTML content to store, or <c>null</c> to remove the entry.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync(Guid itemId, string? contentHtml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the supplied item contents asynchronously in a single batch.
    /// Entries with a <c>null</c> or empty content remove a stored entry.
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
    /// Gets all item identifiers with a stored content asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item identifiers.</returns>
    Task<IReadOnlyList<Guid>> GetItemIdsAsync(CancellationToken cancellationToken = default);
}
