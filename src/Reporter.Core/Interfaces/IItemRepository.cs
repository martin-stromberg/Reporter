// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="Item"/> entities.
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// Gets all items asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of items.</returns>
    Task<IReadOnlyList<Item>> GetAllAsync();

    /// <summary>
    /// Gets the item with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item, or <c>null</c> if not found.</returns>
    Task<Item?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds the specified item asynchronously.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Item item);

    /// <summary>
    /// Updates the specified item asynchronously.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Item item);

    /// <summary>
    /// Deletes the item with the specified identifier asynchronously.
    /// This method is intended for explicit single-item deletions and does not
    /// enforce the saved-for-later invariant; it is not used by automatic cleanup.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Gets all unread items sorted by publication date descending asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread items.</returns>
    Task<IReadOnlyList<Item>> GetUnreadByDateAsync();

    /// <summary>
    /// Gets a paged list of unread items sorted by publication date asynchronously.
    /// </summary>
    /// <param name="page">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <param name="ascending"><c>true</c> to sort oldest first; <c>false</c> (the default) for newest first.</param>
    /// <param name="packet">The optional reading packet limiting the result to its day range; <c>null</c> disables packet filtering.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false, UnreadPacket? packet = null);

    /// <summary>
    /// Resolves the current reading packet of unread items asynchronously: the
    /// calendar day of the first item in the active sort order, extended by the
    /// next occupied day when the anchor day holds fewer items than the packet
    /// threshold. Items without a publication date form a pseudo day that is the
    /// first packet in ascending and the last packet in descending order.
    /// </summary>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <param name="ascending"><c>true</c> for oldest-first; <c>false</c> (the default) for newest-first.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the reading packet, or <c>null</c> when no unread items exist.</returns>
    Task<UnreadPacket?> ResolveUnreadPacketAsync(Guid? categoryId = null, bool ascending = false);

    /// <summary>
    /// Gets the total number of unread items inside the specified reading packet
    /// for the optional category filter — including items beyond the loaded pages.
    /// </summary>
    /// <param name="packet">The reading packet to count.</param>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count.</returns>
    Task<int> GetUnreadPacketCountAsync(UnreadPacket packet, Guid? categoryId = null);

    /// <summary>
    /// Marks all unread items inside the specified reading packet as read for the
    /// optional category filter — including items that were not loaded into the UI.
    /// </summary>
    /// <param name="packet">The reading packet to mark as read.</param>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkUnreadPacketAsReadAsync(UnreadPacket packet, Guid? categoryId = null);

    /// <summary>
    /// Gets the total count of unread items for the optional category filter.
    /// </summary>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count.</returns>
    Task<int> GetUnreadCountAsync(Guid? categoryId = null);

    /// <summary>
    /// Marks all unread items as read for the optional category filter.
    /// </summary>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAllAsReadAsync(Guid? categoryId = null);

    /// <summary>
    /// Toggles the saved-for-later state of the item with the specified identifier.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ToggleSavedForLaterAsync(Guid id);

    /// <summary>
    /// Marks the item with the specified identifier as read.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAsReadAsync(Guid id);

    /// <summary>
    /// Gets all items belonging to the specified feed asynchronously.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the matching items.</returns>
    Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId);

    /// <summary>
    /// Gets a paged list of items belonging to the specified feed, sorted by
    /// publication date descending with the item id as tie-breaker.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="page">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchTerm">An optional term matched against the item title or its tags; <c>null</c> or whitespace disables filtering.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the matching list items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetByFeedAsync(
        Guid feedId,
        int page,
        int pageSize,
        string? searchTerm = null);

    /// <summary>
    /// Gets all items belonging to feeds in the specified category asynchronously.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the matching items.</returns>
    Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId);

    /// <summary>
    /// Gets a paged list of items saved for later sorted by publication date descending asynchronously.
    /// </summary>
    /// <param name="page">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="categoryId">The optional category filter; <c>null</c> returns saved items of all categories.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the saved items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize, Guid? categoryId = null);

    /// <summary>
    /// Gets the number of items saved for later asynchronously.
    /// </summary>
    /// <param name="categoryId">The optional category filter; <c>null</c> counts saved items of all categories.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of saved items.</returns>
    Task<int> GetSavedCountAsync(Guid? categoryId = null);

    /// <summary>
    /// Adds the specified items asynchronously in a single batch.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddRangeAsync(IReadOnlyList<Item> items);

    /// <summary>
    /// Updates the tags of the specified items asynchronously, issuing one
    /// UPDATE statement per entry — the values differ per row, so EF Core
    /// cannot fold them into a single <c>ExecuteUpdate</c>. Only the
    /// <c>tags</c> column is written; all other fields including the stored
    /// content stay untouched. Used by the sync backfill for existing items
    /// that have no stored tags yet.
    /// </summary>
    /// <param name="tagsByItemId">The tag terms to store, keyed by item identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>> tagsByItemId);

    /// <summary>
    /// Deletes all expired items asynchronously.
    /// Only items with <c>IsRead == true</c> are eligible; items with
    /// <c>IsSavedForLater == true</c> are never deleted. The effective timestamp
    /// compared against the cutoff is <c>ReadAt</c>, falling back to
    /// <c>PublishedAt</c>; items with both values <c>null</c> are kept.
    /// </summary>
    /// <param name="cutoff">The cutoff timestamp; items whose effective timestamp is older are deleted.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of deleted items.</returns>
    Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the keyword-filter deletion candidates asynchronously.
    /// Only items with <c>IsRead == true</c> are eligible; items with
    /// <c>IsSavedForLater == true</c> are never returned. The effective timestamp
    /// compared against the cutoff is <c>PublishedAt</c>, falling back to
    /// <c>ReadAt</c>; items with both values <c>null</c> are not returned.
    /// </summary>
    /// <param name="cutoff">The cutoff timestamp; items whose effective timestamp is older are returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the candidate items.</returns>
    Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all items with the specified identifiers asynchronously.
    /// </summary>
    /// <param name="ids">The identifiers of the items to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of deleted items.</returns>
    Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the identifiers of all stored items asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains all item identifiers.</returns>
    Task<IReadOnlyList<Guid>> GetAllIdsAsync(CancellationToken cancellationToken = default);
}
