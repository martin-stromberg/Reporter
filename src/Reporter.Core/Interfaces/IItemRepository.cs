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
    Task<IReadOnlyList<Item>> GetSavedForLaterAsync();
}
