// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ScheduledSyncRunner"/> class.
/// </summary>
public class ScheduledSyncRunnerTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly FakeFeedSyncService _feedSyncService;
    private readonly FakeBackgroundRefreshService _backgroundRefreshService;
    private readonly FakeDebugLogService _debugLogService;
    private readonly ScheduledSyncRunner _runner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledSyncRunnerTests"/> class.
    /// </summary>
    public ScheduledSyncRunnerTests()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _feedSyncService = new FakeFeedSyncService();
        _backgroundRefreshService = new FakeBackgroundRefreshService();
        _debugLogService = new FakeDebugLogService { IsEnabled = true };
        _runner = new ScheduledSyncRunner(_feedSyncService, _settingsRepository, _backgroundRefreshService, _debugLogService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a successful sync returns <c>true</c> and reschedules the
    /// next background refresh task from the persisted settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RunAsync_SyncSucceeds_ReturnsTrueAndReschedules()
    {
        var persisted = await _settingsRepository.GetAsync();

        var success = await _runner.RunAsync();

        Assert.True(success);
        Assert.Equal(1, _feedSyncService.SyncAllCallCount);
        var applied = Assert.Single(_backgroundRefreshService.AppliedSettings);
        Assert.Equal(persisted.RefreshIntervalMinutes, applied.RefreshIntervalMinutes);
        Assert.Equal(persisted.AutoRefreshEnabled, applied.AutoRefreshEnabled);
        var entries = _debugLogService.LoggedEntries;
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal(DebugLogLevel.Info, e.Level));
        Assert.Equal("Background refresh sync started", entries[0].Message);
        Assert.Equal("Background refresh sync finished", entries[1].Message);
    }

    /// <summary>
    /// Verifies that a failing sync returns <c>false</c>, records an error-level
    /// debug log entry and still reschedules the next background refresh task so
    /// the background refresh does not die silently.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RunAsync_SyncFails_ReturnsFalseAndStillReschedules()
    {
        _feedSyncService.SyncAllException = new InvalidOperationException("sync failed");

        var success = await _runner.RunAsync();

        Assert.False(success);
        Assert.Single(_backgroundRefreshService.AppliedSettings);
        var entry = Assert.Single(_debugLogService.LoggedEntries, e => e.Level == DebugLogLevel.Error);
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal("Background refresh sync failed", entry.Message);
        Assert.Contains("sync failed", entry.Details);
    }

    /// <summary>
    /// Verifies that a cancelled sync returns <c>false</c> and still reschedules
    /// the next background refresh task.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RunAsync_SyncCancelled_ReturnsFalseAndStillReschedules()
    {
        _feedSyncService.SyncAllException = new OperationCanceledException();

        var success = await _runner.RunAsync();

        Assert.False(success);
        Assert.Single(_backgroundRefreshService.AppliedSettings);
    }

    /// <summary>
    /// Verifies that a failing rescheduling does not mask a successful sync: the
    /// result still reports the sync outcome and the failure is logged as a warning.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RunAsync_ReschedulingFails_ReturnsTrue()
    {
        _backgroundRefreshService.ApplySettingsException = new InvalidOperationException("scheduling failed");

        var success = await _runner.RunAsync();

        Assert.True(success);
        var entry = Assert.Single(_debugLogService.LoggedEntries, e => e.Level == DebugLogLevel.Warning);
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal("Background refresh rescheduling failed", entry.Message);
        Assert.Contains("scheduling failed", entry.Details);
    }
}
