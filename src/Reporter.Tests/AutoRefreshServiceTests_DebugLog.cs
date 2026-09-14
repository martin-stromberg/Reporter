// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the session debug log instrumentation of the <see cref="AutoRefreshService"/> class.
/// </summary>
public class AutoRefreshServiceTests_DebugLog : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly FakeFeedSyncService _feedSyncService;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeDebugLogService _debugLogService;
    private readonly AutoRefreshService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRefreshServiceTests_DebugLog"/> class.
    /// </summary>
    public AutoRefreshServiceTests_DebugLog()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _feedSyncService = new FakeFeedSyncService();
        _networkStatusService = new FakeNetworkStatusService();
        _timeProvider = new FakeTimeProvider();
        _debugLogService = new FakeDebugLogService { IsEnabled = true };
        _service = new AutoRefreshService(_settingsRepository, _feedSyncService, _networkStatusService, _timeProvider, _debugLogService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a failing startup sync records an error-level entry in the session debug log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task StartAsync_StartupSyncFails_LogsErrorEntry()
    {
        _feedSyncService.SyncAllException = new InvalidOperationException("startup sync failed");
        await TestSettingsHelper.SaveAsync(_settingsRepository, refreshOnStartupEnabled: true);

        await _service.StartAsync();
        await TestWaitHelper.WaitUntilAsync(() => _debugLogService.LoggedEntries.Count == 1);
        await _service.StopAsync();

        var entry = Assert.Single(_debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
        Assert.Contains("startup sync failed", entry.Details);
    }

    /// <summary>
    /// Verifies that a failing periodic sync records an error-level entry in the session debug log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TimerTick_SyncFails_LogsErrorEntry()
    {
        _feedSyncService.SyncAllException = new InvalidOperationException("periodic sync failed");
        await TestSettingsHelper.SaveAsync(_settingsRepository, refreshOnStartupEnabled: false);

        await _service.StartAsync();
        _timeProvider.Advance(TimeSpan.FromMinutes(30));
        await TestWaitHelper.WaitUntilAsync(() => _debugLogService.LoggedEntries.Count >= 1);
        await _service.StopAsync();

        var entry = Assert.Single(_debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
        Assert.Contains("periodic sync failed", entry.Details);
    }
}
