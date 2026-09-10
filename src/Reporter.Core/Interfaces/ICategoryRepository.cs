using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="Category"/> entities.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets all categories asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of categories.</returns>
    Task<IReadOnlyList<Category>> GetAllAsync();

    /// <summary>
    /// Gets all categories with the number of assigned feeds asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of categories with feed counts.</returns>
    Task<IReadOnlyList<CategoryWithCount>> GetAllWithFeedCountAsync();

    /// <summary>
    /// Gets the category with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the category, or <c>null</c> if not found.</returns>
    Task<Category?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds the specified category asynchronously.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Category category);

    /// <summary>
    /// Updates the specified category asynchronously.
    /// </summary>
    /// <param name="category">The category to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Category category);

    /// <summary>
    /// Deletes the category with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
