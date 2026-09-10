using System.Net;
using System.Text;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains integration tests for the <see cref="FeedSyncService"/> class.
/// </summary>
public class FeedSyncServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;
    private readonly SyncLogRepository _syncLogRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests"/> class.
    /// </summary>
    public FeedSyncServiceTests()
    {
        _factory = new TestDbContextFactory();
        _feedRepository = new FeedRepository(_factory);
        _itemRepository = new ItemRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<Guid> SeedFeedAsync(string url = "https://example.com/rss")
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = "Test Feed",
        });
        return feedId;
    }

    private FeedSyncService CreateService(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler((request) =>
        {
            if (statusCode != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(statusCode);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/rss+xml"),
            };
        });
        var httpClient = new HttpClient(handler);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient);
    }

    private FeedSyncService CreateFailingService(Exception exception)
    {
        var handler = new FakeHttpMessageHandler(_ => throw exception);
        var httpClient = new HttpClient(handler);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient);
    }

    private static string RssXml(IEnumerable<(string Title, string Link, string Guid, DateTime? PubDate, string? Description)> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<rss version=\"2.0\">");
        builder.AppendLine("  <channel>");
        builder.AppendLine("    <title>Test Feed</title>");
        foreach (var item in items)
        {
            builder.AppendLine("    <item>");
            builder.AppendLine($"      <title>{item.Title}</title>");
            builder.AppendLine($"      <link>{item.Link}</link>");
            builder.AppendLine($"      <guid>{item.Guid}</guid>");
            if (item.PubDate.HasValue)
            {
                builder.AppendLine($"      <pubDate>{item.PubDate.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}</pubDate>");
            }
            if (item.Description is not null)
            {
                builder.AppendLine($"      <description>{item.Description}</description>");
            }
            builder.AppendLine("    </item>");
        }
        builder.AppendLine("  </channel>");
        builder.AppendLine("</rss>");
        return builder.ToString();
    }

    /// <summary>
    /// Verifies that a valid RSS feed creates new items and updates feed health to OK.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk()
    {
        var feedId = await SeedFeedAsync();
        var now = DateTime.UtcNow;
        var xml = RssXml(
        [
            ("Item One", "https://example.com/1", "guid-1", now.AddHours(-1), "Description one"),
            ("Item Two", "https://example.com/2", "guid-2", now, "Description two"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.True(result.Status == FeedHealth.Ok, $"Unexpected status {result.Status}: {result.Message}");
        Assert.Equal(2, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.GuidOrHash == "guid-1");
        Assert.Contains(items, i => i.GuidOrHash == "guid-2");

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);
        Assert.NotNull(feed.LastCheckedAt);

        var logs = await _syncLogRepository.GetAllAsync();
        Assert.Single(logs);
        Assert.Equal(FeedHealth.Ok, logs[0].Status);
        Assert.NotNull(logs[0].FinishedAt);
    }

    /// <summary>
    /// Verifies that duplicate items are not inserted a second time.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Duplicates_SkipsExistingItems()
    {
        var feedId = await SeedFeedAsync();
        var xml = RssXml(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);
        await service.SyncFeedAsync(feedId);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
    }

    /// <summary>
    /// Verifies that an unreachable feed sets health to Error and preserves existing items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Unreachable_KeepsItemsAndLogsError()
    {
        var feedId = await SeedFeedAsync();
        var itemId = Guid.NewGuid();
        await _itemRepository.AddAsync(new Item
        {
            Id = itemId,
            FeedId = feedId,
            Title = "Existing",
            GuidOrHash = "existing",
            IsRead = false,
            IsSavedForLater = false,
        });

        var service = CreateFailingService(new HttpRequestException("No connection"));
        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        Assert.Equal(0, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("Existing", items[0].Title);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Error, feed.HealthStatus);
    }

    /// <summary>
    /// Verifies that invalid XML sets health to Error and does not remove existing items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_InvalidXml_SetsError()
    {
        var feedId = await SeedFeedAsync();
        var service = CreateService("this is not xml");

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        Assert.Equal(0, result.NewItems);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedHealth.Error, feed?.HealthStatus);

        var logs = await _syncLogRepository.GetAllAsync();
        Assert.Single(logs);
        Assert.Equal(FeedHealth.Error, logs[0].Status);
    }

    /// <summary>
    /// Verifies that a feed returning significantly fewer items sets health to Warning.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FewerItems_SetsWarning()
    {
        var feedId = await SeedFeedAsync();
        var pubDate = DateTime.UtcNow;
        var fourItems = RssXml(
        [
            ("A", "https://example.com/a", "guid-a", pubDate, "A"),
            ("B", "https://example.com/b", "guid-b", pubDate, "B"),
            ("C", "https://example.com/c", "guid-c", pubDate, "C"),
            ("D", "https://example.com/d", "guid-d", pubDate, "D"),
        ]);
        var oneItem = RssXml([
            ("A", "https://example.com/a", "guid-a", pubDate, "A"),
        ]);

        var service = CreateService(fourItems);
        await service.SyncFeedAsync(feedId);

        var secondService = CreateService(oneItem);
        var result = await secondService.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Warning, result.Status);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedHealth.Warning, feed?.HealthStatus);
    }

    /// <summary>
    /// Verifies that no new items for more than 30 days sets health to Warning.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning()
    {
        var feedId = await SeedFeedAsync();
        var pubDate = DateTime.UtcNow.AddDays(-40);
        var xml = RssXml([
            ("Old Item", "https://example.com/old", "guid-old", pubDate, "Old"),
        ]);
        var service = CreateService(xml);
        await service.SyncFeedAsync(feedId);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Warning, result.Status);
    }

    /// <summary>
    /// Verifies that synchronizing all feeds aggregates individual results.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth()
    {
        var feedA = await SeedFeedAsync("https://example.com/a");
        var feedB = await SeedFeedAsync("https://example.com/b");
        var xml = RssXml([
            ("Item", "https://example.com/item", "guid-item", DateTime.UtcNow, "Desc"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncAllAsync();

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="responseFactory">The factory used to create responses.</param>
        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
