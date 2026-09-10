using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="Keyword"/> entities.
/// </summary>
public interface IKeywordRepository
{
    /// <summary>
    /// Gets all keywords asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of keywords.</returns>
    Task<IReadOnlyList<Keyword>> GetAllAsync();

    /// <summary>
    /// Gets the keyword with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The keyword identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the keyword, or <c>null</c> if not found.</returns>
    Task<Keyword?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds the specified keyword asynchronously.
    /// </summary>
    /// <param name="keyword">The keyword to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Keyword keyword);

    /// <summary>
    /// Updates the specified keyword asynchronously.
    /// </summary>
    /// <param name="keyword">The keyword to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Keyword keyword);

    /// <summary>
    /// Deletes the keyword with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The keyword identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
