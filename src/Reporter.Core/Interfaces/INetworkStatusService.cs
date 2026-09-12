// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Abstracts the device network connectivity status so that view models and
/// services can react to online/offline transitions without a MAUI dependency.
/// </summary>
public interface INetworkStatusService
{
    /// <summary>
    /// Occurs when the network connectivity status changes.
    /// Implementations raise this event on the UI thread.
    /// </summary>
    event EventHandler? ConnectivityChanged;

    /// <summary>
    /// Gets a value indicating whether the device currently has internet access.
    /// </summary>
    /// <value><c>true</c> when the device has internet access; otherwise <c>false</c>.</value>
    bool IsOnline { get; }
}
