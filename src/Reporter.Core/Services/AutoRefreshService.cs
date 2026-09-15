// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Periodically invokes <see cref="IFeedSyncService.SyncAllAsync"/> while the app is running.
/// </summary>
public class AutoRefreshService : IAutoRefreshService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFeedSyncService _feedSyncService;
    private readonly INetworkStatusService _networkStatusService;
    private readonly IBackgroundRefreshService _backgroundRefreshService;
    private readonly IDebugLogService? _debugLogService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRefreshService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="feedSyncService">The feed sync service.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    /// <param name="backgroundRefreshService">The OS background refresh gateway that mirrors the auto-refresh settings.</param>
    /// <param name="timeProvider">The time provider used for the refresh timer.</param>
    /// <param name="debugLogService">The optional session debug log service used to record sync failures.</param>
    public AutoRefreshService(ISettingsRepository settingsRepository, IFeedSyncService feedSyncService, INetworkStatusService networkStatusService, IBackgroundRefreshService backgroundRefreshService, TimeProvider? timeProvider = null, IDebugLogService? debugLogService = null)
    {
        _settingsRepository = settingsRepository;
        _feedSyncService = feedSyncService;
        _networkStatusService = networkStatusService;
        _backgroundRefreshService = backgroundRefreshService;
        _debugLogService = debugLogService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        await ApplySettingsAsync(settings);

        if (settings.RefreshOnStartupEnabled && _networkStatusService.IsOnline)
        {
            _ = RunStartupSyncAsync();
        }
    }

    // The startup sync is fire-and-forget: it must neither block nor fail the
    // app start, so errors are logged and swallowed.
    private async Task RunStartupSyncAsync()
    {
        try
        {
            await _feedSyncService.SyncAllAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AutoRefreshService startup sync failed: {ex}");
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Auto refresh sync failed", ex.ToString(), DebugLogLevel.Error);
        }
    }

    /// <inheritdoc />
    public async Task ApplySettingsAsync(Settings settings)
    {
        await _stateLock.WaitAsync();
        try
        {
            await StopLoopAsync();

            // Weiterleitung an das OS-Hintergrundabruf-Gateway: fehlerisoliert,
            // damit ein Gateway-Fehler den Timer-Loop nicht beeintraechtigt, und
            // vor dem AutoRefreshEnabled-Early-Return, damit ein deaktivierter
            // Auto-Refresh den OS-Task abmeldet.
            try
            {
                if (_backgroundRefreshService.IsSupported)
                {
                    await _backgroundRefreshService.ApplySettingsAsync(settings);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoRefreshService background refresh apply failed: {ex}");
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh apply failed", ex.ToString(), DebugLogLevel.Warning);
            }

            if (!settings.AutoRefreshEnabled)
            {
                return;
            }

            var interval = TimeSpan.FromMinutes(SettingsValues.ClampRefreshIntervalMinutes(settings.RefreshIntervalMinutes));
            var cts = new CancellationTokenSource();
            _loopCts = cts;
            _loopTask = RunLoopAsync(interval, cts.Token);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            await StopLoopAsync();
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task StopLoopAsync()
    {
        var cts = _loopCts;
        var loopTask = _loopTask;
        _loopCts = null;
        _loopTask = null;

        try
        {
            cts?.Cancel();

            if (loopTask is not null)
            {
                try
                {
                    await loopTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when the loop is cancelled.
                }
            }
        }
        finally
        {
            cts?.Dispose();
        }
    }

    private async Task RunLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!_networkStatusService.IsOnline)
                {
                    continue;
                }

                try
                {
                    await _feedSyncService.SyncAllAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AutoRefreshService sync failed: {ex}");
                    _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Auto refresh sync failed", ex.ToString(), DebugLogLevel.Error);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the timer is stopped.
        }
    }
}
