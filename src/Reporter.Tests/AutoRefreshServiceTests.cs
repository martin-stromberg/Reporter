using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="AutoRefreshService"/> class.
/// </summary>
public class AutoRefreshServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly FakeFeedSyncService _feedSyncService;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AutoRefreshService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRefreshServiceTests"/> class.
    /// </summary>
    public AutoRefreshServiceTests()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _feedSyncService = new FakeFeedSyncService();
        _networkStatusService = new FakeNetworkStatusService();
        _timeProvider = new FakeTimeProvider();
        _service = new AutoRefreshService(_settingsRepository, _feedSyncService, _networkStatusService, _timeProvider);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private static Settings BuildSettings(bool autoRefreshEnabled, int refreshIntervalMinutes)
    {
        return new Settings
        {
            Id = Settings.DefaultId,
            RetentionDays = 30,
            AutoMarkReadDelaySeconds = 5,
            NotificationsEnabled = true,
            NotificationSummaryEnabled = false,
            AutoRefreshEnabled = autoRefreshEnabled,
            RefreshIntervalMinutes = refreshIntervalMinutes,
        };
    }

    private async Task SaveSettingsAsync(bool autoRefreshEnabled, int refreshIntervalMinutes)
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            NotificationSummaryEnabled = settings.NotificationSummaryEnabled,
            QuietHoursStart = settings.QuietHoursStart,
            QuietHoursEnd = settings.QuietHoursEnd,
            AutoRefreshEnabled = autoRefreshEnabled,
            RefreshIntervalMinutes = refreshIntervalMinutes,
            Theme = settings.Theme,
        });
    }

    /// <summary>
    /// Verifies that StartAsync invokes SyncAllAsync after the configured interval elapsed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task StartAsync_InvokesSyncAfterInterval()
    {
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        await _service.StopAsync();

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that ApplySettingsAsync stops the timer when automatic refresh is disabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ApplySettings_Disabled_Stops()
    {
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);

        await _service.ApplySettingsAsync(BuildSettings(autoRefreshEnabled: false, refreshIntervalMinutes: 15));
        _timeProvider.Advance(TimeSpan.FromMinutes(30));
        await Task.Delay(50);

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that ApplySettingsAsync restarts the timer with the new interval.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ApplySettings_ChangesInterval()
    {
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);

        await _service.ApplySettingsAsync(BuildSettings(autoRefreshEnabled: true, refreshIntervalMinutes: 30));
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await Task.Delay(50);
        Assert.Equal(1, _feedSyncService.SyncAllCallCount);

        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 2);
        await _service.StopAsync();

        Assert.Equal(2, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that an exception in SyncAllAsync does not stop the timer loop.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncThrows_LoopContinues()
    {
        _feedSyncService.SyncAllException = new InvalidOperationException("sync failed");
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 2);
        await _service.StopAsync();

        Assert.Equal(2, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that timer ticks elapsed while a sync is still running never start a parallel
    /// sync: the loop awaits each <c>SyncAllAsync</c> call sequentially and
    /// <see cref="PeriodicTimer"/> coalesces missed ticks, so no second sync can begin.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CoalescedTicks_DoNotStartParallelSync()
    {
        var blocker = new TaskCompletionSource<bool>();
        _feedSyncService.SyncAllBlocker = blocker;
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        _timeProvider.Advance(TimeSpan.FromMinutes(30));
        await Task.Delay(50);

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);

        blocker.SetResult(true);
        await _service.StopAsync();
    }

    /// <summary>
    /// Verifies that a timer tick is skipped without calling SyncAllAsync while offline.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Tick_WhenOffline_SkipsSyncAll()
    {
        _networkStatusService.IsOnline = false;
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await Task.Delay(100);
        await _service.StopAsync();

        Assert.Equal(0, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that the next tick synchronizes again once the network is back.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Tick_WhenBackOnline_ResumesSync()
    {
        _networkStatusService.IsOnline = false;
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await Task.Delay(100);
        Assert.Equal(0, _feedSyncService.SyncAllCallCount);

        _networkStatusService.IsOnline = true;
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        await _service.StopAsync();

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that an out-of-range persisted interval is clamped instead of crashing the service.
    /// </summary>
    /// <param name="persistedMinutes">The persisted interval in minutes.</param>
    /// <param name="expectedMinutes">The expected clamped interval in minutes.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99999, 1440)]
    public async Task InvalidInterval_Clamped(int persistedMinutes, int expectedMinutes)
    {
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: persistedMinutes);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(expectedMinutes));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        await _service.StopAsync();

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that repeated start/stop cycles leave the service in a clean working state:
    /// each stop fully tears down the loop and each start creates a fresh, ticking timer.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task StartStop_RepeatedCycles_RestartsCleanly()
    {
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        for (var i = 0; i < 3; i++)
        {
            await _service.StartAsync();
            await _service.StopAsync();
        }

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);
        await _service.StopAsync();

        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
    }

    /// <summary>
    /// Verifies that two overlapping ApplySettingsAsync calls leave exactly one active
    /// timer loop instead of orphaning a loop that keeps ticking (review finding:
    /// stop and restart were not atomic). The first apply is held in its stop phase by
    /// a blocked sync so the second apply provably overlaps with it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ApplySettings_Concurrent_LeavesSingleActiveLoop()
    {
        var blocker = new TaskCompletionSource<bool>();
        _feedSyncService.SyncAllBlocker = blocker;
        await SaveSettingsAsync(autoRefreshEnabled: true, refreshIntervalMinutes: 15);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount == 1);

        var apply1 = _service.ApplySettingsAsync(BuildSettings(autoRefreshEnabled: true, refreshIntervalMinutes: 15));
        var apply2 = _service.ApplySettingsAsync(BuildSettings(autoRefreshEnabled: true, refreshIntervalMinutes: 15));
        blocker.SetResult(true);
        await Task.WhenAll(apply1, apply2);

        _feedSyncService.SyncAllBlocker = null;
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        await TestWaitHelper.WaitUntilAsync(() => _feedSyncService.SyncAllCallCount >= 2);
        await Task.Delay(100);
        await _service.StopAsync();

        Assert.Equal(2, _feedSyncService.SyncAllCallCount);
    }
}
