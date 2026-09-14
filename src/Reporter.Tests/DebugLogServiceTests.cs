// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;
using DebugLogEntryEntity = Reporter.Data.Entities.DebugLogEntry;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="DebugLogService"/> class.
/// </summary>
public class DebugLogServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DebugLogRepository _debugLogRepository;
    private readonly SettingsRepository _settingsRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogServiceTests"/> class.
    /// </summary>
    public DebugLogServiceTests()
    {
        _factory = new TestDbContextFactory();
        _debugLogRepository = new DebugLogRepository(_factory);
        _settingsRepository = new SettingsRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that BeginSessionAsync clears the previous session but keeps its
    /// error entries, so a crash report stays sendable after a restart.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task BeginSessionAsync_ResetsPreviousEntries_KeepsErrors()
    {
        await _debugLogRepository.AddAsync(new DebugLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            Level = DebugLogLevel.Error,
            Category = DebugLogCategory.Exception,
            Message = "previous session crash",
        });
        await _debugLogRepository.AddAsync(new DebugLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddMinutes(-4),
            Level = DebugLogLevel.Info,
            Category = DebugLogCategory.Lifecycle,
            Message = "previous session info",
        });
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);

        await service.BeginSessionAsync();

        var entries = await _debugLogRepository.GetAllAsync();
        var entry = Assert.Single(entries);
        Assert.Equal("previous session crash", entry.Message);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
    }

    /// <summary>
    /// Verifies that BeginSessionAsync loads the persisted collection switch.
    /// </summary>
    /// <param name="persisted">The persisted collection switch value.</param>
    /// <param name="expected">The expected <see cref="IDebugLogService.IsEnabled"/> value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task BeginSessionAsync_LoadsEnabledState(bool persisted, bool expected)
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: persisted);
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);

        await service.BeginSessionAsync();

        Assert.Equal(expected, service.IsEnabled);
    }

    /// <summary>
    /// Verifies that BeginSessionAsync writes a lifecycle start entry when collection is enabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task BeginSessionAsync_Enabled_WritesStartEntry()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true);
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);

        await service.BeginSessionAsync();

        var entries = await _debugLogRepository.GetAllAsync();
        var entry = Assert.Single(entries);
        Assert.Equal(DebugLogCategory.Lifecycle, entry.Category);
        Assert.Equal(DebugLogLevel.Info, entry.Level);
        Assert.Equal("Debug session started", entry.Message);
    }

    /// <summary>
    /// Verifies that LogAsync is a no-op while collection is disabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LogAsync_Disabled_WritesNothing()
    {
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);
        await service.BeginSessionAsync();

        await service.LogAsync(DebugLogCategory.Sync, "must not be stored");

        var entries = await _debugLogRepository.GetAllAsync();
        Assert.Empty(entries);
    }

    /// <summary>
    /// Verifies that LogAsync persists the entry while collection is enabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LogAsync_Enabled_WritesEntry()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero));
        var service = new DebugLogService(_debugLogRepository, _settingsRepository, timeProvider);
        service.SetEnabled(true);
        await TestWaitHelper.WaitUntilAsync(async () => (await _debugLogRepository.GetAllAsync()).Count == 1);

        await service.LogAsync(DebugLogCategory.Sync, "sync failed", "boom", DebugLogLevel.Error);

        var entries = await _debugLogRepository.GetAllAsync();
        var entry = Assert.Single(entries, e => e.Message == "sync failed");
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal("boom", entry.Details);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, entry.Timestamp);
    }

    /// <summary>
    /// Verifies that LogAsync trims the session log to <see cref="DebugLogService.MaxStoredEntries"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LogAsync_TrimsToMaxStoredEntries()
    {
        // Seed timestamps in the past so the freshly written entry is the newest one.
        var now = DateTime.UtcNow.AddMinutes(-10);
        await using (var context = _factory.CreateDbContext())
        {
            var entities = Enumerable
                .Range(0, DebugLogService.MaxStoredEntries)
                .Select(i => new DebugLogEntryEntity
                {
                    Id = Guid.NewGuid(),
                    Timestamp = now.AddSeconds(i),
                    Level = DebugLogLevel.Info,
                    Category = DebugLogCategory.Lifecycle,
                    Message = $"old-{i}",
                });
            context.DebugLogEntries.AddRange(entities);
            await context.SaveChangesAsync();
        }

        var service = new DebugLogService(_debugLogRepository, _settingsRepository);
        service.SetEnabled(true);

        await service.LogAsync(DebugLogCategory.Sync, "newest entry");

        var entries = await _debugLogRepository.GetAllAsync();
        Assert.Equal(DebugLogService.MaxStoredEntries, entries.Count);
        Assert.Equal("newest entry", entries[0].Message);
        Assert.DoesNotContain(entries, e => e.Message == "old-0");
    }

    /// <summary>
    /// Verifies that repository failures are swallowed and never reach the caller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LogAsync_RepositoryError_DoesNotThrow()
    {
        var service = new DebugLogService(new ThrowingDebugLogRepository(), _settingsRepository);

        var exception = await Record.ExceptionAsync(() => service.BeginSessionAsync());
        Assert.Null(exception);
        Assert.False(service.IsEnabled);

        service.SetEnabled(true);
        exception = await Record.ExceptionAsync(() => service.LogAsync(DebugLogCategory.Sync, "fails"));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that enabling the collection via SetEnabled writes a lifecycle transition entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetEnabled_EnableTransition_WritesLifecycleEntry()
    {
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);

        service.SetEnabled(true);

        await TestWaitHelper.WaitUntilAsync(async () => (await _debugLogRepository.GetAllAsync()).Count == 1);
        var entries = await _debugLogRepository.GetAllAsync();
        Assert.Equal(DebugLogCategory.Lifecycle, entries[0].Category);
        Assert.Equal("Debug collection enabled", entries[0].Message);
    }

    /// <summary>
    /// Verifies that disabling the collection writes no transition entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetEnabled_DisableTransition_WritesNothing()
    {
        var service = new DebugLogService(_debugLogRepository, _settingsRepository);
        service.SetEnabled(true);
        await TestWaitHelper.WaitUntilAsync(async () => (await _debugLogRepository.GetAllAsync()).Count == 1);

        service.SetEnabled(false);
        await Task.Delay(100);

        var entries = await _debugLogRepository.GetAllAsync();
        Assert.Single(entries);
    }

    private sealed class ThrowingDebugLogRepository : IDebugLogRepository
    {
        public Task<IReadOnlyList<DebugLogEntry>> GetAllAsync()
        {
            throw new InvalidOperationException("database unavailable");
        }

        public Task<IReadOnlyList<DebugLogEntry>> GetLatestAsync(int maxEntries)
        {
            throw new InvalidOperationException("database unavailable");
        }

        public Task AddAsync(DebugLogEntry entry)
        {
            throw new InvalidOperationException("database unavailable");
        }

        public Task DeleteAllExceptErrorsAsync()
        {
            throw new InvalidOperationException("database unavailable");
        }

        public Task TrimToLatestAsync(int maxEntries)
        {
            throw new InvalidOperationException("database unavailable");
        }
    }
}
