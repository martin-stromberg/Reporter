// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the per-feed synchronization lock of the <see cref="FeedSyncService"/> class.
/// </summary>
public class FeedSyncServiceTests_Concurrency : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeItemContentStore _contentStore;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly KeywordFilter _keywordFilter;
    private readonly FakeFeedIconService _feedIconService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests_Concurrency"/> class.
    /// </summary>
    public FeedSyncServiceTests_Concurrency()
    {
        _factory = new TestDbContextFactory();
        _contentStore = new FakeItemContentStore();
        _feedRepository = new FeedRepository(_factory, _contentStore);
        _itemRepository = new ItemRepository(_factory, _contentStore);
        _syncLogRepository = new SyncLogRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _keywordFilter = new KeywordFilter(_keywordRepository, new KeywordMatcher());
        _feedIconService = new FakeFeedIconService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that two overlapping syncs of the same feed are serialized by the
    /// per-feed lock: while the first sync is blocked inside its HTTP request, the
    /// second call must not reach the HTTP layer. Both runs then complete
    /// successfully without duplicate items or a persisted error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ConcurrentCallsOnSameFeed_AreSerialized()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var now = DateTime.UtcNow;
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", now, "Description one"),
            ("Item Two", "https://example.com/2", "guid-2", now, "Description two"),
        ]);

        var requestCount = 0;
        var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new FakeHttpMessageHandler(async _ =>
        {
            if (Interlocked.Increment(ref requestCount) == 1)
            {
                firstRequestStarted.TrySetResult();
                await releaseFirstRequest.Task;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/rss+xml"),
            };
        });
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
            new FakeItemImageService(),
            _contentStore);

        var firstSync = service.SyncFeedAsync(feedId);
        await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var secondSync = service.SyncFeedAsync(feedId);

        try
        {
            // While the first sync still holds the feed lock, the second call
            // waits on it and must not issue its own HTTP request.
            await Task.Delay(250);
            Assert.Equal(1, Volatile.Read(ref requestCount));
            Assert.False(secondSync.IsCompleted);
        }
        finally
        {
            releaseFirstRequest.TrySetResult();
        }

        var results = await Task.WhenAll(firstSync, secondSync);

        Assert.All(results, r => Assert.Equal(FeedHealth.Ok, r.Status));
        Assert.Equal(2, results.Sum(r => r.NewItems));
        Assert.Equal(2, Volatile.Read(ref requestCount));

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal(2, items.Count);
        Assert.Single(items, i => i.GuidOrHash == "guid-1");
        Assert.Single(items, i => i.GuidOrHash == "guid-2");

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);
        Assert.Null(feed.LastMessageKind);
        Assert.Null(feed.LastMessage);
    }
}
