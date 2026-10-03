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
    private readonly FakeItemContentStore _contentStore;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly KeywordFilter _keywordFilter;
    private readonly FakeFeedIconService _feedIconService;
    private readonly FakeItemImageService _imageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncServiceTests"/> class.
    /// </summary>
    public FeedSyncServiceTests()
    {
        _factory = new TestDbContextFactory();
        _contentStore = new FakeItemContentStore();
        _feedRepository = new FeedRepository(_factory, _contentStore);
        _itemRepository = new ItemRepository(_factory, _contentStore);
        _syncLogRepository = new SyncLogRepository(_factory);
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _keywordFilter = new KeywordFilter(_keywordRepository, new KeywordMatcher());
        _feedIconService = new FakeFeedIconService();
        _imageService = new FakeItemImageService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private FeedSyncService CreateService(string content, HttpStatusCode statusCode = HttpStatusCode.OK, INotificationService? notificationService = null, INetworkStatusService? networkStatusService = null)
    {
        var handler = new FakeHttpMessageHandler(_ =>
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
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, notificationService ?? new FakeNotificationService(), networkStatusService ?? new FakeNetworkStatusService(), _keywordFilter, _feedIconService, _imageService, _contentStore);
    }

    private FeedSyncService CreateFailingService(Exception exception, INetworkStatusService? networkStatusService = null)
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromException<HttpResponseMessage>(exception));
        var httpClient = new HttpClient(handler);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, new FakeNotificationService(), networkStatusService ?? new FakeNetworkStatusService(), _keywordFilter, _feedIconService, _imageService, _contentStore);
    }

    private FeedSyncService CreateServiceWithImageDownload(string feedXml, Func<HttpRequestMessage, HttpResponseMessage> imageResponder)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true)
            {
                return imageResponder(request);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(feedXml, Encoding.UTF8, "application/rss+xml"),
            };
        });
        var httpClient = new HttpClient(handler);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, new FakeNotificationService(), new FakeNetworkStatusService(), _keywordFilter, _feedIconService, new ItemImageService(httpClient), _contentStore);
    }

    private NotificationService CreateNotificationService(FakeLocalNotificationService localNotificationService)
    {
        return new NotificationService(_settingsRepository, _keywordFilter, localNotificationService);
    }

    // The second sync shrinks the feed from four items to one, which trips
    // the fewer-items warning. Returns the feed id, the four-item service
    // (for a follow-up sync that lifts the warning again) and the shrinking
    // sync's result.
    private async Task<(Guid FeedId, FeedSyncService FullService, SyncResult ShrinkResult)> SeedThenShrinkFeedAsync()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var pubDate = DateTime.UtcNow;
        var fourItems = TestFeedXml.Rss(
        [
            ("A", "https://example.com/a", "guid-a", pubDate, "A"),
            ("B", "https://example.com/b", "guid-b", pubDate, "B"),
            ("C", "https://example.com/c", "guid-c", pubDate, "C"),
            ("D", "https://example.com/d", "guid-d", pubDate, "D"),
        ]);
        var oneItem = TestFeedXml.Rss([
            ("A", "https://example.com/a", "guid-a", pubDate, "A"),
        ]);

        var service = CreateService(fourItems);
        await service.SyncFeedAsync(feedId);

        var shrinkResult = await CreateService(oneItem).SyncFeedAsync(feedId);
        return (feedId, service, shrinkResult);
    }

    // The first sync stores one item published 40 days ago; the resync finds
    // no new items and trips the no-recent-items warning. Returns the feed id
    // and the resync's result.
    private async Task<(Guid FeedId, SyncResult StaleResult)> SeedStaleFeedThenResyncAsync()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var pubDate = DateTime.UtcNow.AddDays(-40);
        var xml = TestFeedXml.Rss([
            ("Old Item", "https://example.com/old", "guid-old", pubDate, "Old"),
        ]);
        var service = CreateService(xml);
        await service.SyncFeedAsync(feedId);

        var staleResult = await service.SyncFeedAsync(feedId);
        return (feedId, staleResult);
    }

    /// <summary>
    /// Verifies that a valid RSS feed creates new items and updates feed health to OK.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var now = DateTime.UtcNow;
        var xml = TestFeedXml.Rss(
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.Rss(
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
    /// Verifies that items duplicated within the same feed document are inserted only once.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var now = DateTime.UtcNow;
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", now, "Description one"),
            ("Item One Copy", "https://example.com/1", "guid-1", now, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
    }

    /// <summary>
    /// Verifies that a sync with mixed new and existing items batch-inserts only the new ones.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var now = DateTime.UtcNow;
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Existing",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = now,
        });
        var xml = TestFeedXml.Rss(
        [
            ("Existing", "https://example.com/1", "guid-1", now, "Description one"),
            ("New One", "https://example.com/2", "guid-2", now, "Description two"),
            ("New Two", "https://example.com/3", "guid-3", now, "Description three"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal(3, items.Count);
        Assert.Single(items, i => i.GuidOrHash == "guid-1");
        Assert.Contains(items, i => i.GuidOrHash == "guid-2");
        Assert.Contains(items, i => i.GuidOrHash == "guid-3");
    }

    /// <summary>
    /// Verifies that an unreachable feed sets health to Error and preserves existing items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Unreachable_KeepsItemsAndLogsError()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
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
        var (feedId, _, result) = await SeedThenShrinkFeedAsync();

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
        var (_, result) = await SeedStaleFeedThenResyncAsync();

        Assert.Equal(FeedHealth.Warning, result.Status);
    }

    /// <summary>
    /// Verifies that the fewer-items warning persists its warning kind and a
    /// technical message on the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FewerItems_PersistsWarningMessage()
    {
        var (feedId, _, result) = await SeedThenShrinkFeedAsync();

        Assert.Equal(FeedHealth.Warning, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Warning, feed.HealthStatus);
        Assert.Equal(FeedSyncWarningKind.FewerItems, feed.LastMessageKind);
        Assert.False(string.IsNullOrWhiteSpace(feed.LastMessage));
    }

    /// <summary>
    /// Verifies that the stale-feed warning persists its warning kind and a
    /// technical message on the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage()
    {
        var (feedId, result) = await SeedStaleFeedThenResyncAsync();

        Assert.Equal(FeedHealth.Warning, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Warning, feed.HealthStatus);
        Assert.Equal(FeedSyncWarningKind.NoRecentItems, feed.LastMessageKind);
        Assert.False(string.IsNullOrWhiteSpace(feed.LastMessage));
    }

    /// <summary>
    /// Verifies that a successful sync after a warning clears the persisted
    /// warning kind and message.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage()
    {
        var (feedId, service, _) = await SeedThenShrinkFeedAsync();

        var feedAfterWarning = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncWarningKind.FewerItems, feedAfterWarning?.LastMessageKind);

        // Serving the full item set again lifts the fewer-items warning; the
        // shared message fields are cleared on the Ok status.
        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);
        Assert.Null(feed.LastMessageKind);
        Assert.Null(feed.LastMessage);
    }

    /// <summary>
    /// Verifies that a failed sync after a warning replaces the warning kind
    /// with the classified error kind — the shared message fields keep the
    /// message of the latest status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage()
    {
        var (feedId, _, _) = await SeedThenShrinkFeedAsync();

        var feedAfterWarning = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncWarningKind.FewerItems, feedAfterWarning?.LastMessageKind);

        var failingService = CreateFailingService(new HttpRequestException("No connection"));
        var result = await failingService.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Error, feed.HealthStatus);
        Assert.Equal(FeedSyncErrorKind.Network, feed.LastMessageKind);
        Assert.Equal("Synchronization failed: No connection", feed.LastMessage);
    }

    /// <summary>
    /// Verifies that synchronizing all feeds aggregates individual results.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth()
    {
        var feedA = await TestDataSeeder.SeedFeedAsync(_feedRepository, "https://example.com/a");
        var feedB = await TestDataSeeder.SeedFeedAsync(_feedRepository, "https://example.com/b");
        var xml = TestFeedXml.Rss([
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var feedBefore = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedBefore);
        var service = CreateService(TestFeedXml.Rss([
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(TestFeedXml.Rss(
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(TestFeedXml.Rss(
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var localNotifications = new FakeLocalNotificationService();
        var notificationService = CreateNotificationService(localNotifications);
        var xml = TestFeedXml.Rss([
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository, notificationsEnabled: false);
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(TestFeedXml.Rss([
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var notificationService = new FakeNotificationService { Exception = new InvalidOperationException("notification failed") };
        var service = CreateService(TestFeedXml.Rss([
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
        var xml = TestFeedXml.Rss([
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
        var xml = TestFeedXml.Rss([
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
        var xml = TestFeedXml.Rss([
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.Rss([
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ], channelTitle: "Different Document Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Test Feed", feed.Title);
    }

    /// <summary>
    /// Verifies that an item whose title matches a configured keyword is not stored
    /// and does not appear in the unread list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordTitleMatch_NotSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater mit bis zu 2.600 MBit/s", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("guid-1", items[0].GuidOrHash);

        var unread = await _itemRepository.GetUnreadByDateAsync();
        Assert.DoesNotContain(unread, i => i.Title.Contains("Anzeige"));
    }

    /// <summary>
    /// Verifies that an item matching a keyword via its HTML content is not stored.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordContentHtmlMatch_NotSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Gewinnspiel" });
        var xml = TestFeedXml.Rss(
        [
            ("Regular Title", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Jetzt am Gewinnspiel teilnehmen"),
            ("Other Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("guid-1", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that a non-matching item is stored normally when keywords are configured.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordNoMatch_SavesNormally()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var xml = TestFeedXml.Rss(
        [
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
    }

    /// <summary>
    /// Verifies that an empty keyword list does not change the previous sync behavior.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_EmptyKeywords_SavesAll()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: Something", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal(2, items.Count);
    }

    /// <summary>
    /// Verifies that a keyword-filtered item does not trigger a notification through
    /// the real <see cref="NotificationService"/> decision chain.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordFiltered_NotNotified()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var localNotifications = new FakeLocalNotificationService();
        var service = CreateService(TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]), notificationService: CreateNotificationService(localNotifications));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);
        var notification = Assert.Single(localNotifications.ShownNotifications);
        Assert.Equal("Regular Article", notification.Body);
    }

    /// <summary>
    /// Verifies that a filtered item stays filtered on a follow-up sync and is not
    /// stored as a duplicate.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);
        await service.SyncFeedAsync(feedId);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("guid-1", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that the sync log message reports the number of filtered items and
    /// that the <see cref="SyncResult"/> carries the same text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordFiltered_LogCountsFiltered()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
            ("Another Article", "https://example.com/2", "guid-2", DateTime.UtcNow, "Description two"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(2, result.NewItems);
        Assert.NotNull(result.Message);
        Assert.Contains(", 1 filtered", result.Message);

        var logs = await _syncLogRepository.GetAllAsync();
        var log = Assert.Single(logs);
        Assert.Equal(result.Message, log.Message);
    }

    /// <summary>
    /// Verifies that a keyword match in an Atom 1.0 document is not stored, covering
    /// the parsing path of the reported example feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        var xml = TestFeedXml.Atom(
        [
            ("Anzeige: WLAN-Repeater mit bis zu 2.600 MBit/s", "https://example.com/ad", "atom-ad", DateTime.UtcNow, "Sponsored content"),
            ("Regular Article", "https://example.com/1", "atom-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("atom-1", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that a feed-scoped keyword filters the matching item of its own feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FeedKeywordMatch_NotSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige", FeedId = feedId });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater mit bis zu 2.600 MBit/s", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("guid-1", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that a feed-scoped keyword does not affect another feed: the same
    /// matching article title is stored for a feed without that keyword.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FeedKeyword_OtherFeedUnaffected()
    {
        var feedWithKeyword = await TestDataSeeder.SeedFeedAsync(_feedRepository, "https://example.com/a");
        var feedWithoutKeyword = await TestDataSeeder.SeedFeedAsync(_feedRepository, "https://example.com/b");
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige", FeedId = feedWithKeyword });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedWithoutKeyword);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedWithoutKeyword);
        Assert.Single(items);
        Assert.Equal("guid-ad", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that the effective keyword list of a feed is the union of global
    /// and feed keywords: items matching either list are discarded.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FeedKeyword_CombinedWithGlobal()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Anzeige" });
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Gewinnspiel", FeedId = feedId });
        var xml = TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Jetzt am Gewinnspiel teilnehmen", "https://example.com/raffle", "guid-raffle", DateTime.UtcNow, "Raffle"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Single(items);
        Assert.Equal("guid-1", items[0].GuidOrHash);
    }

    /// <summary>
    /// Verifies that a successful sync backfills the favicon URL of a feed that
    /// has none, using the feed URL's authority when the document declares no
    /// alternate site link.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        _feedIconService.NextResult = "https://example.com/favicon.ico";
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("https://example.com/favicon.ico", feed.FaviconUrl);
        Assert.Equal("https://example.com", Assert.Single(_feedIconService.RequestedSiteUrls));
    }

    /// <summary>
    /// Verifies that a successful sync does not touch an already stored favicon
    /// and skips the lookup entirely.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingFavicon_SkipsLookup()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Test Feed",
            NotificationsEnabled = true,
            FaviconUrl = "https://example.com/stored.ico",
        });
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("https://example.com/stored.ico", feed.FaviconUrl);
        Assert.Empty(_feedIconService.RequestedSiteUrls);
    }

    /// <summary>
    /// Verifies that a failing favicon lookup does not affect the sync result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FaviconLookupFails_SyncStillSucceeds()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        _feedIconService.NextException = new InvalidOperationException("lookup failed");
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Null(feed.FaviconUrl);
    }

    /// <summary>
    /// Verifies that the cancellation token passed to
    /// <see cref="FeedSyncService.SyncFeedAsync"/> is forwarded to the favicon
    /// lookup, so an aborted synchronization does not wait for the icon HTTP
    /// roundtrips.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_MissingFavicon_ForwardsCancellationTokenToIconLookup()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);
        using var cts = new CancellationTokenSource();

        var result = await service.SyncFeedAsync(feedId, cts.Token);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var receivedToken = Assert.Single(_feedIconService.ReceivedCancellationTokens);
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// Verifies that a failed sync persists the classified error kind and the
    /// technical message on the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Failure_PersistsErrorKindAndMessage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateFailingService(new HttpRequestException("No connection"));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedSyncErrorKind.Network, feed.LastMessageKind);
        Assert.Equal("Synchronization failed: No connection", feed.LastMessage);
    }

    /// <summary>
    /// Verifies that a successful sync clears the message fields persisted by a
    /// previous failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Success_ClearsLastMessage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var failingService = CreateFailingService(new HttpRequestException("No connection"));
        await failingService.SyncFeedAsync(feedId);

        var feedAfterFailure = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feedAfterFailure?.LastMessageKind);

        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);
        Assert.Null(feed.LastMessageKind);
        Assert.Null(feed.LastMessage);
    }

    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> against an
    /// <c>http</c> feed URL is classified as <see cref="FeedSyncErrorKind.InsecureHttpBlocked"/>,
    /// covering the App Transport Security failure on iOS/MacCatalyst.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository, "http://example.com/feed");
        var service = CreateFailingService(new HttpRequestException("Blocked by ATS"));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncErrorKind.InsecureHttpBlocked, feed?.LastMessageKind);
    }

    /// <summary>
    /// Verifies that a non-success HTTP response is classified as
    /// <see cref="FeedSyncErrorKind.HttpStatus"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_HttpStatusError_ClassifiedAsHttpStatus()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateService(string.Empty, statusCode: HttpStatusCode.InternalServerError);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncErrorKind.HttpStatus, feed?.LastMessageKind);
    }

    /// <summary>
    /// Verifies that an unparseable feed document is classified as
    /// <see cref="FeedSyncErrorKind.Parse"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_InvalidXml_ClassifiedAsParse()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateService("this is not xml");

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncErrorKind.Parse, feed?.LastMessageKind);
    }

    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> without a status code
    /// against an <c>https</c> feed URL is classified as
    /// <see cref="FeedSyncErrorKind.Network"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_HttpsFeedNetworkFailure_ClassifiedAsNetwork()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateFailingService(new HttpRequestException("No connection"));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Error, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedSyncErrorKind.Network, feed?.LastMessageKind);
    }

    /// <summary>
    /// Verifies that an Atom 0.3 document (namespace
    /// <c>http://purl.org/atom/ns#</c>) is synchronized like RSS/Atom 1.0:
    /// items are persisted with title, link, publication date and identifier,
    /// and feed health is set to OK.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var now = DateTime.UtcNow;
        var xml = TestFeedXml.Atom03(
        [
            ("Item One", "https://example.com/1", "tag:example.com,2024:1", now.AddHours(-2), now.AddHours(-1), "&lt;p&gt;One&lt;/p&gt;"),
            ("Item Two", "https://example.com/2", "tag:example.com,2024:2", now, now, null),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.True(result.Status == FeedHealth.Ok, $"Unexpected status {result.Status}: {result.Message}");
        Assert.Equal(2, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i =>
            i.Title == "Item One" && i.Link == "https://example.com/1" &&
            i.GuidOrHash == "tag:example.com,2024:1" && i.PublishedAt.HasValue);
        Assert.Contains(items, i =>
            i.Title == "Item Two" && i.Link == "https://example.com/2" &&
            i.GuidOrHash == "tag:example.com,2024:2" && i.PublishedAt.HasValue);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);

        var logs = await _syncLogRepository.GetAllAsync();
        var log = Assert.Single(logs);
        Assert.Equal(FeedHealth.Ok, log.Status);
        Assert.NotNull(log.FinishedAt);
    }

    /// <summary>
    /// Verifies that the Atom 0.3 <c>issued</c> element of an entry is mapped to
    /// <see cref="Item.PublishedAt"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Atom03_MapsIssuedToPublishedAt()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var issued = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var xml = TestFeedXml.Atom03(
        [
            ("Item One", "https://example.com/1", "tag:example.com,2024:1", issued, null, null),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var items = await _itemRepository.GetByFeedAsync(feedId);
        var item = Assert.Single(items);
        Assert.Equal(issued, item.PublishedAt);
    }

    /// <summary>
    /// Verifies that a placeholder title (the feed URL stored as title) is
    /// replaced by the title of an Atom 0.3 feed document on the first sync.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_Atom03_PlaceholderTitle_UpdatesTitleFromFeedDocument()
    {
        const string url = "https://example.com/atom03";
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = url,
            NotificationsEnabled = true,
        });
        var xml = TestFeedXml.Atom03(
        [
            ("Item One", "https://example.com/1", "tag:example.com,2024:1", DateTime.UtcNow, null, null),
        ], feedTitle: "Resolved Atom03 Title");
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(feed);
        Assert.Equal("Resolved Atom03 Title", feed.Title);
    }

    /// <summary>
    /// Verifies that an existing item without stored content (e.g. after a
    /// restore without the content database) is not duplicated but gets the
    /// freshly downloaded content backfilled into the content store.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_BackfillsMissingContent()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Item One",
            Link = "https://example.com/1",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _itemRepository.AddAsync(existing);

        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Restored description"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);

        var items = await _itemRepository.GetByFeedAsync(feedId);
        var item = Assert.Single(items);
        Assert.Equal("Restored description", item.ContentHtml);
        Assert.Equal("Restored description", await _contentStore.GetAsync(existing.Id));
    }

    /// <summary>
    /// Verifies that an existing item that already has stored content is left
    /// untouched: the deduplication skips it and the backfill does not
    /// overwrite the stored content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_KeepsExistingContent()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Item One",
            Link = "https://example.com/1",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>stored</p>",
        };
        await _itemRepository.AddAsync(existing);

        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Changed description"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Equal("<p>stored</p>", await _contentStore.GetAsync(existing.Id));
    }

    /// <summary>
    /// Verifies that an enclosure image of a new item is downloaded during the
    /// sync and stored in the content store.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_EnclosureImage_StoresImageLocally()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var imageBytes = new byte[] { 1, 2, 3 };
        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/img.png", "image/png"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(imageBytes));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var items = await _itemRepository.GetByFeedAsync(feedId);
        var item = Assert.Single(items);
        var image = await _contentStore.GetImageAsync(item.Id);
        Assert.NotNull(image);
        Assert.Equal(imageBytes, image.Data);
        Assert.Equal("image/png", image.ContentType);
        Assert.Equal("https://example.com/img.png", image.Url);
    }

    /// <summary>
    /// Verifies that the first <c>&lt;img src&gt;</c> of the item content is
    /// downloaded and stored when no dedicated image field exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_FirstImgSrc_StoresImageLocally()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var imageBytes = new byte[] { 4, 5, 6 };
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "&lt;p&gt;A &lt;img src=&quot;https://example.com/img.png&quot; /&gt;&lt;/p&gt;"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(imageBytes));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        var image = await _contentStore.GetImageAsync(item.Id);
        Assert.NotNull(image);
        Assert.Equal(imageBytes, image.Data);
        Assert.Equal("https://example.com/img.png", image.Url);
    }

    /// <summary>
    /// Verifies that a relative <c>&lt;img src&gt;</c> is resolved against the
    /// item link before the download.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_RelativeImgSrc_DownloadsResolved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var imageBytes = new byte[] { 7 };
        var xml = TestFeedXml.Rss(
        [
            ("Item One", "https://example.com/articles/1", "guid-1", DateTime.UtcNow, "&lt;p&gt;&lt;img src=&quot;/images/hero.png&quot; /&gt;&lt;/p&gt;"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(imageBytes));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        var image = await _contentStore.GetImageAsync(item.Id);
        Assert.NotNull(image);
        Assert.Equal("https://example.com/images/hero.png", image.Url);
    }

    /// <summary>
    /// Verifies that a failed image download leaves the item stored without an
    /// image while the sync stays healthy (remote image URL fallback).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ImageDownloadFails_SyncStillSucceeds()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/missing.png", "image/png"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);
        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Null(await _contentStore.GetImageAsync(item.Id));

        var feed = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(FeedHealth.Ok, feed?.HealthStatus);
    }

    /// <summary>
    /// Verifies that an image response exceeding the 5 MB cap is discarded —
    /// exercised through the real <see cref="ItemImageService"/> limit.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_OversizedImage_StoresNoImage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/big.png", "image/png"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(new byte[(5 * 1024 * 1024) + 1]));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var item = Assert.Single(await _itemRepository.GetByFeedAsync(feedId));
        Assert.Null(await _contentStore.GetImageAsync(item.Id));
    }

    /// <summary>
    /// Verifies that an existing item without a stored image gets the freshly
    /// resolved image backfilled while its stored content stays untouched.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingItemsWithoutImage_BackfillsImage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Item One",
            Link = "https://example.com/1",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>stored</p>",
        };
        await _itemRepository.AddAsync(existing);

        var imageBytes = new byte[] { 1, 2 };
        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/img.png", "image/png"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(imageBytes));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Equal("<p>stored</p>", await _contentStore.GetAsync(existing.Id));
        var image = await _contentStore.GetImageAsync(existing.Id);
        Assert.NotNull(image);
        Assert.Equal(imageBytes, image.Data);
    }

    /// <summary>
    /// Verifies that an existing item without stored content and without a
    /// stored image gets both backfilled through a single merged entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingItemWithoutContentAndImage_BackfillsBoth()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Item One",
            Link = "https://example.com/1",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _itemRepository.AddAsync(existing);

        var imageBytes = new byte[] { 3 };
        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Restored description", "https://example.com/img.png", "image/png"),
        ]);
        var service = CreateServiceWithImageDownload(xml, _ => TestHttpResponses.Png(imageBytes));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(0, result.NewItems);
        Assert.Equal("Restored description", await _contentStore.GetAsync(existing.Id));
        var image = await _contentStore.GetImageAsync(existing.Id);
        Assert.NotNull(image);
        Assert.Equal(imageBytes, image.Data);
    }

    /// <summary>
    /// Verifies that an existing item with a stored image is not downloaded
    /// again on the next sync.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SyncFeedAsync_ExistingImage_NotRedownloaded()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var existing = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Item One",
            Link = "https://example.com/1",
            GuidOrHash = "guid-1",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>stored</p>",
            Image = new ItemImage([9], "image/png", "https://example.com/img.png"),
        };
        await _itemRepository.AddAsync(existing);

        var xml = TestFeedXml.RssWithEnclosure(
        [
            ("Item One", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/img.png", "image/png"),
        ]);
        var service = CreateService(xml);

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Empty(_imageService.RequestedUrls);
    }
}
