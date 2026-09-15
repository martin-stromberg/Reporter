// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Abstracts the platform facility for OS-scheduled background refreshes (for
/// example the iOS <c>BGTaskScheduler</c> with a <c>BGAppRefreshTask</c>).
/// Implementations are no-ops on platforms that do not support background refresh.
/// </summary>
public interface IBackgroundRefreshService
{
    /// <summary>
    /// Gets a value indicating whether the current platform supports OS-scheduled
    /// background refreshes.
    /// </summary>
    /// <value><c>true</c> when the platform can run background refresh tasks; otherwise <c>false</c>.</value>
    bool IsSupported { get; }

    /// <summary>
    /// Schedules or cancels the OS background refresh task according to the
    /// specified settings: the task is submitted with
    /// <c>Settings.RefreshIntervalMinutes</c> as the earliest begin date while
    /// <c>Settings.AutoRefreshEnabled</c> is set, and cancelled otherwise.
    /// </summary>
    /// <param name="settings">The settings to apply.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplySettingsAsync(Settings settings, CancellationToken cancellationToken = default);
}
