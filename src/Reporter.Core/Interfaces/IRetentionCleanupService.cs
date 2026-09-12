// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Removes items that exceeded the configured retention period.
/// </summary>
public interface IRetentionCleanupService
{
    /// <summary>
    /// Deletes all read items older than the configured retention period.
    /// Items saved for later are never deleted.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of deleted items.</returns>
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
