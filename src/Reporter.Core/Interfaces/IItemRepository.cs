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
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false);

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
    /// <returns>A task that represents the asynchronous operation. The task result contains the saved items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize);

    /// <summary>
    /// Adds the specified items asynchronously in a single batch.
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddRangeAsync(IReadOnlyList<Item> items);

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
}
