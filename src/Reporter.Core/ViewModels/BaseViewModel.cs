// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Core.ViewModels;

/// <summary>
/// Provides a base implementation for all view models.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private INetworkStatusService? _networkStatusService;
    private bool _isConnectivityTracked;
    private bool _isOnline;
    private bool _isSyncInProgress;

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

    /// <summary>
    /// Updates the view model's synchronization-in-progress flag. Implementations
    /// that use <see cref="RunFeedSyncAsync"/> map this to their bound
    /// <c>IsSyncing</c> property (e.g. <c>RefreshView.IsRefreshing</c>).
    /// </summary>
    /// <param name="isSyncing">The new flag value.</param>
    protected virtual void SetIsSyncing(bool isSyncing)
    {
    }

    /// <summary>
    /// Reports a synchronization error to the view model's sync error channel;
    /// an empty message clears it.
    /// </summary>
    /// <param name="message">The localized error message or an empty string.</param>
    protected virtual void ReportSyncError(string message)
    {
    }

    /// <summary>
    /// Runs the feed synchronization exactly once at a time. The reentrancy
    /// guard uses a private runtime flag because the syncing flag is preset to
    /// <see langword="true"/> by the <c>RefreshView.IsRefreshing</c> TwoWay
    /// binding before the refresh command executes; guarding on the bound
    /// property would turn every pull-to-refresh gesture into a no-op. The same
    /// preset value is reset in the offline early-return path so the refresh
    /// indicator does not hang.
    /// </summary>
    /// <param name="syncAction">The synchronization operation to run.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is
    /// <see langword="true"/> when the sync action ran — also on failure — and
    /// <see langword="false"/> when it was skipped because a sync is already in
    /// progress or the device is offline.
    /// </returns>
    protected async Task<bool> RunFeedSyncAsync(Func<Task<SyncResult>> syncAction)
    {
        if (_isSyncInProgress)
        {
            return false;
        }

        if (!IsOnline)
        {
            SetIsSyncing(false);
            return false;
        }

        _isSyncInProgress = true;
        SetIsSyncing(true);
        ReportSyncError(string.Empty);

        try
        {
            var result = await syncAction();
            if (result.Status == FeedHealth.Error)
            {
                // The technical detail stays in the SyncLog (persisted via
                // FeedSyncService.UpdateLogAsync); the UI shows the localized
                // generic message instead of raw English/exception text.
                ReportSyncError(AppResources.SyncStatusError);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RunFeedSyncAsync failed: {ex}");
            ReportSyncError(AppResources.SyncStatusError);
            return true;
        }
        finally
        {
            _isSyncInProgress = false;
            SetIsSyncing(false);
        }
    }

    /// <summary>
    /// Loads all categories preceded by the pseudo-entry
    /// (<see cref="Guid.Empty"/>/<see cref="AppResources.CategoryNone"/>) that
    /// represents "no category" in pickers.
    /// </summary>
    /// <param name="categoryRepository">The category repository.</param>
    /// <returns>The categories including the leading none-entry.</returns>
    protected static async Task<ObservableCollection<Category>> LoadCategoriesWithNoneAsync(
        ICategoryRepository categoryRepository)
    {
        var categories = new List<Category>
        {
            new Category { Id = Guid.Empty, Name = AppResources.CategoryNone },
        };
        categories.AddRange(await categoryRepository.GetAllAsync());
        return new ObservableCollection<Category>(categories);
    }

    private void HandleConnectivityChanged(object? sender, EventArgs e)
    {
        RefreshConnectivityStatus();
    }
}
