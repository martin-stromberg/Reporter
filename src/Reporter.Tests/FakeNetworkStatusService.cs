// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="INetworkStatusService"/> fake with a settable online state.
/// </summary>
public sealed class FakeNetworkStatusService : INetworkStatusService
{
    /// <inheritdoc />
    public event EventHandler? ConnectivityChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the fake reports the device as online.
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// Raises the <see cref="ConnectivityChanged"/> event synchronously.
    /// </summary>
    public void RaiseConnectivityChanged()
    {
        ConnectivityChanged?.Invoke(this, EventArgs.Empty);
    }
}
