// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
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
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests"/> class.
    /// </summary>
    public FeedSyncServiceTests()
    {
        _factory = new TestDbContextFactory();
        _feedRepository = new FeedRepository(_factory);
        _itemRepository = new ItemRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<Guid> SeedFeedAsync(string url = "https://example.com/rss", bool notificationsEnabled = true)
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = "Test Feed",
            NotificationsEnabled = notificationsEnabled,
        });
        return feedId;
    }

    private FeedSyncService CreateService(string content, HttpStatusCode statusCode = HttpStatusCode.OK, INotificationService? notificationService = null, INetworkStatusService? networkStatusService = null)
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
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, notificationService ?? new FakeNotificationService(), networkStatusService ?? new FakeNetworkStatusService());
    }

    private FeedSyncService CreateFailingService(Exception exception, INetworkStatusService? networkStatusService = null)
    {
        var handler = new FakeHttpMessageHandler(_ => throw exception);
        var httpClient = new HttpClient(handler);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, new FakeNotificationService(), networkStatusService ?? new FakeNetworkStatusService());
    }

    private NotificationService CreateNotificationService(FakeLocalNotificationService localNotificationService)
    {
        return new NotificationService(_settingsRepository, _keywordRepository, new KeywordMatcher(), localNotificationService);
    }

    private static string RssXml(IEnumerable<(string Title, string Link, string Guid, DateTime? PubDate, string? Description)> items, string channelTitle = "Test Feed")
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<rss version=\"2.0\">");
        builder.AppendLine("  <channel>");
        builder.AppendLine($"    <title>{channelTitle}</title>");
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

    /// <summary>
    /// Verifies that SyncAllAsync returns an offline error without writing a sync log
    /// or changing feed health while there is no internet connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog()
    {
        var feedId = await SeedFeedAsync();
        var feedBefore = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedBefore);
        var service = CreateService(RssXml([
            ("Item", "https://example.com/item", "guid-item", DateTime.UtcNow, "Desc"),
        ]), networkStatusService: new FakeNetworkStatusService { IsOnline = false });

        var result = await service.SyncAllAsync();

        Assert.Equal(FeedHealth.Error, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Equal(AppResources.OfflineHint, result.Message);

        var logs = await _syncLogRepository.GetAllAsync();
        Assert.Empty(logs);

        var feedAfter = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedAfter);
        Assert.Equal(feedBefore.HealthStatus, feedAfter.HealthStatus);
    }

    /// <summary>
    /// Verifies that SyncFeedAsync returns an offline error without writing a sync log,
    /// changing feed health or performing an HTTP request while offline.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange()
    {
        var feedId = await SeedFeedAsync();
        var feedBefore = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedBefore);
        var service = CreateFailingService(
            new InvalidOperationException("HTTP must not be called while offline"),
            new FakeNetworkStatusService { IsOnline = false });

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Equal(AppResources.OfflineHint, result.Message);

        var logs = await _syncLogRepository.GetAllAsync();
        Assert.Empty(logs);

        var feedAfter = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedAfter);
        Assert.Equal(feedBefore.HealthStatus, feedAfter.HealthStatus);
    }

    /// <summary>
    /// Verifies that newly synced items trigger a notification per item through the
    /// real <see cref="NotificationService"/> decision chain.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NewItems_NotifiesWithFeedAndItems()
    {
        var feedId = await SeedFeedAsync();
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(RssXml(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
            ("Item Two", "https://example.com/2", "guid-2", DateTime.UtcNow, "Description two"),
        ]), notificationService: CreateNotificationService(localNotifications));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);
        Assert.Equal(2, localNotifications.ShownNotifications.Count);
        Assert.All(localNotifications.ShownNotifications, n => Assert.Equal("Test Feed", n.Title));
        Assert.Contains(localNotifications.ShownNotifications, n => n.Body == "Item One");
        Assert.Contains(localNotifications.ShownNotifications, n => n.Body == "Item Two");

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.All(localNotifications.ShownNotifications, n => Assert.Contains(items, i => i.Id.ToString() == n.Identifier));
    }

    /// <summary>
    /// Verifies that the summary mode sends exactly one notification for multiple new items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification()
    {
        var feedId = await SeedFeedAsync();
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(RssXml(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
            ("Item Two", "https://example.com/2", "guid-2", DateTime.UtcNow, "Description two"),
        ]), notificationService: CreateNotificationService(localNotifications));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);
        var notification = Assert.Single(localNotifications.ShownNotifications);
        Assert.Equal("Test Feed", notification.Title);
        Assert.Contains("2", notification.Body);
        Assert.StartsWith($"{feedId}-", notification.Identifier);
    }

    /// <summary>
    /// Verifies that a second sync without new items does not notify again.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NoNewItems_DoesNotNotify()
    {
        var feedId = await SeedFeedAsync();
        var localNotifications = new FakeLocalNotificationService();
        var notificationService = CreateNotificationService(localNotifications);
        var xml = RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml, notificationService: notificationService);
        await service.SyncFeedAsync(feedId);
        Assert.Single(localNotifications.ShownNotifications);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Single(localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that a feed with notifications disabled does not trigger notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FeedDisabled_NoNotifications()
    {
        var feedId = await SeedFeedAsync(notificationsEnabled: false);
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]), notificationService: CreateNotificationService(localNotifications));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);
        Assert.Empty(localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that a failure in the notification path does not affect the sync result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NotificationThrows_SyncStillSucceeds()
    {
        var feedId = await SeedFeedAsync();
        var notificationService = new FakeNotificationService { Exception = new InvalidOperationException("notification failed") };
        var service = CreateService(RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]), notificationService: notificationService);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);
        Assert.Single(notificationService.Calls);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedHealth.Ok, feed?.HealthStatus);
        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
    }

    /// <summary>
    /// Verifies that a placeholder title (the feed URL stored as title) is replaced
    /// by the feed document title on the first sync.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument()
    {
        const string url = "https://example.com/rss";
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = url,
            NotificationsEnabled = true,
        });
        var xml = RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ], channelTitle: "Resolved Feed Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Resolved Feed Title", feed.Title);
    }

    /// <summary>
    /// Verifies that a placeholder title in the form the direct-add flow produces
    /// (the host name stored as title) is replaced by the feed document title on
    /// the first sync — same as the URL-as-title placeholder.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument()
    {
        const string url = "https://example.com/rss";
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = "example.com",
            NotificationsEnabled = true,
        });
        var xml = RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ], channelTitle: "Resolved Feed Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Resolved Feed Title", feed.Title);
    }

    /// <summary>
    /// Verifies that a placeholder title in the form the file-name fallback
    /// produces (the last URL path segment stored as title) is replaced by the
    /// feed document title on the first sync.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument()
    {
        const string url = "https://heise.de/rss/heise-atom.xml";
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = "heise-atom.xml",
            NotificationsEnabled = true,
        });
        var xml = RssXml([
            ("Item One", "https://heise.de/1", "guid-1", DateTime.UtcNow, "Description one"),
        ], channelTitle: "Resolved Feed Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Resolved Feed Title", feed.Title);
    }

    /// <summary>
    /// Verifies that an explicitly set feed title is never overwritten by the
    /// feed document title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle()
    {
        var feedId = await SeedFeedAsync();
        var xml = RssXml([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ], channelTitle: "Different Document Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Test Feed", feed.Title);
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
