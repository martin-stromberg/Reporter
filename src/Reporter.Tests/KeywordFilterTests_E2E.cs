// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains end-to-end tests for the keyword filter covering the full user flow
/// from adding a keyword in the settings over the feed sync to the unread list.
/// </summary>
public class KeywordFilterTests_E2E : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FeedRepository _feedRepository;
    private readonly ItemRepository _itemRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly FakeLocalNotificationService _localNotificationService;
    private readonly KeywordFilter _keywordFilter;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordFilterTests_E2E"/> class.
    /// </summary>
    public KeywordFilterTests_E2E()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _feedRepository = new FeedRepository(_factory);
        _itemRepository = new ItemRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _localNotificationService = new FakeLocalNotificationService();
        _keywordFilter = new KeywordFilter(_keywordRepository, new KeywordMatcher());
        _viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, new FakeAutoRefreshService(), new FakeAppThemeService());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private FeedSyncService CreateService(string content)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/rss+xml"),
        });
        var httpClient = new HttpClient(handler);
        var notificationService = new NotificationService(_settingsRepository, _keywordFilter, _localNotificationService);
        return new FeedSyncService(_feedRepository, _itemRepository, _syncLogRepository, httpClient, notificationService, new FakeNetworkStatusService(), _keywordFilter, new FakeFeedIconService());
    }

    private async Task AddKeywordViaSettingsAsync(string keywordText)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = keywordText;
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Verifies the end-to-end flow: a keyword added in the settings filters a matching
    /// item out of the sync, so it never appears in the unread list while a
    /// non-matching item is listed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordFilter_MatchedItemNotInUnreadList()
    {
        await AddKeywordViaSettingsAsync("Anzeige");
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateService(TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater mit bis zu 2.600 MBit/s", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.Equal(1, result.NewItems);

        var unread = await _itemRepository.GetUnreadByDateAsync(0, 50);
        var listed = Assert.Single(unread);
        Assert.Equal("Regular Article", listed.Title);
    }

    /// <summary>
    /// Verifies the end-to-end flow: a keyword-filtered item produces no notification
    /// while the non-matching item is still announced.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordFilter_MatchedItemNoNotification()
    {
        await AddKeywordViaSettingsAsync("Anzeige");
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateService(TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        var notification = Assert.Single(_localNotificationService.ShownNotifications);
        Assert.Equal("Regular Article", notification.Body);
    }

    /// <summary>
    /// Verifies the end-to-end flow: the persisted sync log message reports the
    /// number of filtered items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordFilter_SyncLogReportsFiltered()
    {
        await AddKeywordViaSettingsAsync("Anzeige");
        var feedId = await TestDataSeeder.SeedFeedAsync(_feedRepository);
        var service = CreateService(TestFeedXml.Rss(
        [
            ("Anzeige: WLAN-Repeater", "https://example.com/ad", "guid-ad", DateTime.UtcNow, "Sponsored"),
            ("Regular Article", "https://example.com/1", "guid-1", DateTime.UtcNow, "Description one"),
        ]));

        var result = await service.SyncFeedAsync(feedId);

        Assert.Equal(FeedHealth.Ok, result.Status);
        Assert.NotNull(result.Message);
        Assert.Contains(", 1 filtered", result.Message);

        var logs = await _syncLogRepository.GetAllAsync();
        var log = Assert.Single(logs);
        Assert.Equal(result.Message, log.Message);
    }
}
