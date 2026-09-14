// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines the write side of the session debug log: unhandled exceptions,
/// synchronization errors and important lifecycle events recorded while debug
/// collection is enabled. Implementations never throw.
/// </summary>
public interface IDebugLogService
{
    /// <summary>
    /// Gets a value indicating whether debug collection is currently active.
    /// </summary>
    /// <value><c>true</c> when log entries are being recorded; otherwise <c>false</c>.</value>
    bool IsEnabled { get; }

    /// <summary>
    /// Starts a new debug session: removes the entries of the previous session
    /// except <see cref="DebugLogLevel.Error"/> entries (crash reports stay
    /// sendable after a restart), loads the persisted
    /// <c>Settings.DebugCollectionEnabled</c> switch into <see cref="IsEnabled"/>
    /// and writes a start entry when collection is active.
    /// Must be called after the database migrations have been applied.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables debug collection for the running session, writing a
    /// lifecycle transition entry when collection is activated.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable collection; <c>false</c> to disable it.</param>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Records a debug log entry. The call is a no-op while <see cref="IsEnabled"/>
    /// is <c>false</c> and never throws.
    /// </summary>
    /// <param name="category">The entry category (see <see cref="DebugLogCategory"/>).</param>
    /// <param name="message">The log message.</param>
    /// <param name="details">Optional details such as an exception dump (<c>exception.ToString()</c>).</param>
    /// <param name="level">The severity level (see <see cref="DebugLogLevel"/>).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogAsync(string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken cancellationToken = default);
}
