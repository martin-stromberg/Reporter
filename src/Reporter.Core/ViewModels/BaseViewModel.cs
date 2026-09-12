using CommunityToolkit.Mvvm.ComponentModel;
using Reporter.Core.Interfaces;

namespace Reporter.Core.ViewModels;

/// <summary>
/// Provides a base implementation for all view models.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private INetworkStatusService? _networkStatusService;
    private bool _isConnectivityTracked;
    private bool _isOnline;

    /// <summary>
    /// Gets a value indicating whether the device currently has internet access.
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set => SetProperty(ref _isOnline, value);
    }

    /// <summary>
    /// Stores the connectivity service and initializes <see cref="IsOnline"/>
    /// without subscribing to change events.
    /// </summary>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    protected void InitConnectivity(INetworkStatusService networkStatusService)
    {
        _networkStatusService = networkStatusService;
        IsOnline = networkStatusService.IsOnline;
    }

    /// <summary>
    /// Initializes connectivity state and subscribes to change events.
    /// </summary>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    protected void TrackConnectivity(INetworkStatusService networkStatusService)
    {
        InitConnectivity(networkStatusService);
        TrackConnectivity();
    }

    /// <summary>
    /// Subscribes to <see cref="INetworkStatusService.ConnectivityChanged"/> of the
    /// service previously passed to <see cref="InitConnectivity"/>. Safe to call repeatedly.
    /// </summary>
    protected void TrackConnectivity()
    {
        if (_networkStatusService is null || _isConnectivityTracked)
        {
            return;
        }

        _isConnectivityTracked = true;
        _networkStatusService.ConnectivityChanged += HandleConnectivityChanged;
    }

    /// <summary>
    /// Unsubscribes from connectivity change events. Safe to call repeatedly.
    /// </summary>
    protected void UntrackConnectivity()
    {
        if (_networkStatusService is null || !_isConnectivityTracked)
        {
            return;
        }

        _isConnectivityTracked = false;
        _networkStatusService.ConnectivityChanged -= HandleConnectivityChanged;
    }

    /// <summary>
    /// Re-reads the status from the service, updates <see cref="IsOnline"/> and
    /// invokes <see cref="OnConnectivityChanged"/> when the value changed.
    /// </summary>
    protected void RefreshConnectivityStatus()
    {
        if (_networkStatusService is null)
        {
            return;
        }

        var isOnline = _networkStatusService.IsOnline;
        if (IsOnline == isOnline)
        {
            return;
        }

        IsOnline = isOnline;
        OnConnectivityChanged(isOnline);
    }

    /// <summary>
    /// Called after <see cref="IsOnline"/> changed because of a connectivity event
    /// or a <see cref="RefreshConnectivityStatus"/> call.
    /// </summary>
    /// <param name="isOnline">The new online state.</param>
    protected virtual void OnConnectivityChanged(bool isOnline)
    {
    }

    private void HandleConnectivityChanged(object? sender, EventArgs e)
    {
        RefreshConnectivityStatus();
    }
}
