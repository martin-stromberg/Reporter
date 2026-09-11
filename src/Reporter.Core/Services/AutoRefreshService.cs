using System.Diagnostics;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Periodically invokes <see cref="IFeedSyncService.SyncAllAsync"/> while the app is running.
/// </summary>
public class AutoRefreshService : IAutoRefreshService
{
    private const int MinRefreshIntervalMinutes = 1;
    private const int MaxRefreshIntervalMinutes = 1440;

    private readonly ISettingsRepository _settingsRepository;
    private readonly IFeedSyncService _feedSyncService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private int _syncRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRefreshService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="feedSyncService">The feed sync service.</param>
    /// <param name="timeProvider">The time provider used for the refresh timer.</param>
    public AutoRefreshService(ISettingsRepository settingsRepository, IFeedSyncService feedSyncService, TimeProvider? timeProvider = null)
    {
        _settingsRepository = settingsRepository;
        _feedSyncService = feedSyncService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        await ApplySettingsAsync(settings);
    }

    /// <inheritdoc />
    public async Task ApplySettingsAsync(Settings settings)
    {
        await _stateLock.WaitAsync();
        try
        {
            await StopLoopAsync();

            if (!settings.AutoRefreshEnabled)
            {
                return;
            }

            var interval = TimeSpan.FromMinutes(Math.Clamp(settings.RefreshIntervalMinutes, MinRefreshIntervalMinutes, MaxRefreshIntervalMinutes));
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
                if (Interlocked.Exchange(ref _syncRunning, 1) == 1)
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
                }
                finally
                {
                    Interlocked.Exchange(ref _syncRunning, 0);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the timer is stopped.
        }
    }
}
