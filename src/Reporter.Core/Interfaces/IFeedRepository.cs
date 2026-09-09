using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="Feed"/> entities.
/// </summary>
public interface IFeedRepository
{
    /// <summary>
    /// Gets all feeds asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of feeds.</returns>
    Task<IReadOnlyList<Feed>> GetAllAsync();

    /// <summary>
    /// Gets the feed with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The feed identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the feed, or <c>null</c> if not found.</returns>
    Task<Feed?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds the specified feed asynchronously.
    /// </summary>
    /// <param name="feed">The feed to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Feed feed);

    /// <summary>
    /// Updates the specified feed asynchronously.
    /// </summary>
    /// <param name="feed">The feed to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(Feed feed);

    /// <summary>
    /// Deletes the feed with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The feed identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
