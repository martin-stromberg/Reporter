// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to the session <see cref="DebugLogEntry"/> entities.
/// </summary>
public interface IDebugLogRepository
{
    /// <summary>
    /// Gets all debug log entries asynchronously, ordered by descending timestamp.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of debug log entries.</returns>
    Task<IReadOnlyList<DebugLogEntry>> GetAllAsync();

    /// <summary>
    /// Gets the newest <paramref name="maxEntries"/> debug log entries asynchronously, ordered by descending timestamp.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to return.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newest debug log entries.</returns>
    Task<IReadOnlyList<DebugLogEntry>> GetLatestAsync(int maxEntries);

    /// <summary>
    /// Adds the specified debug log entry asynchronously.
    /// </summary>
    /// <param name="entry">The debug log entry to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(DebugLogEntry entry);

    /// <summary>
    /// Deletes all debug log entries except those with level <see cref="DebugLogLevel.Error"/>
    /// asynchronously. Used for the session reset so that crash and error reports of the
    /// previous session survive an app restart and remain sendable.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAllExceptErrorsAsync();

    /// <summary>
    /// Deletes all but the newest <paramref name="maxEntries"/> debug log entries asynchronously.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to keep.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TrimToLatestAsync(int maxEntries);
}
