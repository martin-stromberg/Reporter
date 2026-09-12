// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Periodically synchronizes all feeds in the background while the app is running.
/// </summary>
public interface IAutoRefreshService
{
    /// <summary>
    /// Loads the settings and starts the background refresh timer if enabled.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the specified settings by restarting the background refresh timer
    /// with the configured interval, or stopping it when automatic refresh is disabled.
    /// </summary>
    /// <param name="settings">The settings to apply.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplySettingsAsync(Settings settings);

    /// <summary>
    /// Stops the background refresh timer.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync();
}
