// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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
    /// Gets the keywords assigned to the specified feed scope asynchronously.
    /// </summary>
    /// <param name="feedId">The feed identifier, or <c>null</c> for global keywords only.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the keywords of the requested scope ordered by text.</returns>
    Task<IReadOnlyList<Keyword>> GetByFeedAsync(Guid? feedId);

    /// <summary>
    /// Gets the effective keyword list for the specified feed asynchronously (global keywords plus the feed's own keywords).
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the union of global and feed keywords ordered by text.</returns>
    Task<IReadOnlyList<Keyword>> GetEffectiveForFeedAsync(Guid feedId);

    /// <summary>
    /// Checks whether at least one keyword exists asynchronously, in any scope (global or feed-scoped).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when any keyword row exists.</returns>
    Task<bool> AnyAsync();

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
