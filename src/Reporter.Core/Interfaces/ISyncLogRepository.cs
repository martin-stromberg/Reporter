// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="SyncLog"/> entities.
/// </summary>
public interface ISyncLogRepository
{
    /// <summary>
    /// Gets all sync log entries asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of sync logs.</returns>
    Task<IReadOnlyList<SyncLog>> GetAllAsync();

    /// <summary>
    /// Gets the newest <paramref name="maxEntries"/> sync log entries asynchronously, ordered by descending <c>StartedAt</c>.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to return.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newest sync logs.</returns>
    Task<IReadOnlyList<SyncLog>> GetLatestAsync(int maxEntries);

    /// <summary>
    /// Gets the sync log with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The sync log identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the sync log, or <c>null</c> if not found.</returns>
    Task<SyncLog?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds the specified sync log asynchronously.
    /// </summary>
    /// <param name="syncLog">The sync log to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(SyncLog syncLog);

    /// <summary>
    /// Updates the specified sync log asynchronously.
    /// </summary>
    /// <param name="syncLog">The sync log to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(SyncLog syncLog);

    /// <summary>
    /// Deletes the sync log with the specified identifier asynchronously.
    /// </summary>
    /// <param name="id">The sync log identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
