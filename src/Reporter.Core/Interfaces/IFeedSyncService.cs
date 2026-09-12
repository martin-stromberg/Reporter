// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Synchronizes RSS/Atom feeds and stores new items.
/// </summary>
public interface IFeedSyncService
{
    /// <summary>
    /// Synchronizes the feed with the specified identifier.
    /// </summary>
    /// <param name="feedId">The feed identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the synchronization result.</returns>
    Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes all configured feeds.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the combined synchronization result.</returns>
    Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default);
}
