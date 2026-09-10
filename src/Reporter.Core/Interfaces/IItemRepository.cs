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
    /// Gets a paged list of unread items sorted by publication date descending asynchronously.
    /// </summary>
    /// <param name="page">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="categoryId">The optional category to filter by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread items for the page.</returns>
    Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null);

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
    /// Gets all items saved for later asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the saved items.</returns>
    Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync();

    /// <summary>
    /// Gets the item with the specified GUID or hash for the specified feed asynchronously.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="guidOrHash">The original GUID or hash.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item, or <c>null</c> if not found.</returns>
    Task<Item?> GetByGuidOrHashAsync(Guid feedId, string guidOrHash);
}
