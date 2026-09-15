// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Runs the feed synchronization for a launched OS background refresh task and
/// reschedules the next task. The platform task handler (for example the iOS
/// <c>AppDelegate</c>) invokes this runner when the system starts the task.
/// </summary>
public interface IScheduledSyncRunner
{
    /// <summary>
    /// Runs the feed synchronization and reschedules the next background refresh
    /// task from the persisted settings, including after a failed or cancelled sync.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation (for example the task expiration handler).</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when the sync succeeded; intended for the platform task completion call.</returns>
    Task<bool> RunAsync(CancellationToken cancellationToken = default);
}
