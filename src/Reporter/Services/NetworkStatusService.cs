// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Reporter.Core.Interfaces;

namespace Reporter.Services;

/// <summary>
/// Reports the device network connectivity status via <see cref="Connectivity"/>.
/// </summary>
public class NetworkStatusService : INetworkStatusService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkStatusService"/> class.
    /// </summary>
    public NetworkStatusService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <inheritdoc />
    public event EventHandler? ConnectivityChanged;

    /// <inheritdoc />
    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var handler = ConnectivityChanged;
        if (handler is null)
        {
            return;
        }

        // ConnectivityChanged kann auf einem Hintergrund-Thread ausloesen;
        // die ViewModels aktualisieren UI-gebundene Properties.
        MainThread.BeginInvokeOnMainThread(() => handler(this, EventArgs.Empty));
    }
}
