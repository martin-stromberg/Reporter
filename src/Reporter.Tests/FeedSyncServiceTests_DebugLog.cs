// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the session debug log instrumentation of the <see cref="FeedSyncService"/> class.
/// </summary>
public class FeedSyncServiceTests_DebugLog : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly KeywordFilter _keywordFilter;
    private readonly FakeFeedIconService _feedIconService;
    private readonly FakeDebugLogService _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests_DebugLog"/> class.
    /// </summary>
    public FeedSyncServiceTests_DebugLog()
    {
        _factory = new TestDbContextFactory();
        _feedRepository = new FeedRepository(_factory);
        _itemRepository = new ItemRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _keywordFilter = new KeywordFilter(_keywordRepository, new KeywordMatcher());
        _feedIconService = new FakeFeedIconService();
        _debugLogService = new FakeDebugLogService { IsEnabled = true };
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a failing feed sync records an error-level entry in the session debug log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Failure_LogsErrorEntry()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        var httpClient = new HttpClient(handler);
        var service = new FeedSyncService(
            _feedRepository,
            _itemRepository,
            _syncLogRepository,
            httpClient,
            new FakeNotificationService(),
            new FakeNetworkStatusService(),
            _keywordFilter,
            _feedIconService,
            _debugLogService);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var entry = Assert.Single(_debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Sync, entry.Category);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
        Assert.Contains("connection refused", entry.Details);
    }

    /// <summary>
    /// Verifies that a failing feed sync works unchanged without a debug log service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        var httpClient = new HttpClient(handler);
        var service = new FeedSyncService(
            _feedRepository,
            _itemRepository,
            _syncLogRepository,
            httpClient,
            new FakeNotificationService(),
            new FakeNetworkStatusService(),
            _keywordFilter,
            _feedIconService);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
    }
}
