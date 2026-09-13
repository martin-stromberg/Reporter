// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains unit tests for the <see cref="NotificationService"/> decision chain.
/// </summary>
public class NotificationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly KeywordFilter _keywordFilter;
    private readonly FakeLocalNotificationService _localNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationServiceTests"/> class.
    /// </summary>
    public NotificationServiceTests()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _keywordFilter = new KeywordFilter(_keywordRepository, new KeywordMatcher());
        _localNotifications = new FakeLocalNotificationService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a feed with notifications disabled does not send notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_FeedDisabled_SendsNothing()
    {
        var feed = CreateFeed(notificationsEnabled: false);
        var item = CreateItem(feed.Id, "Breaking News");

        await CreateService().NotifyNewItemsAsync(feed, [item]);

        Assert.Empty(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that the global notifications switch suppresses all notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_GlobalDisabled_SendsNothing()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationsEnabled: false);
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Breaking News");

        await CreateService().NotifyNewItemsAsync(feed, [item]);

        Assert.Empty(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that notifications are suppressed while the local time is inside quiet hours.
    /// </summary>
    /// <param name="hour">The local hour of the day to test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(23)]
    [InlineData(6)]
    public async Task NotifyNewItemsAsync_WithinQuietHours_SendsNothing(int hour)
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, quietHoursStart: TimeSpan.FromHours(22), quietHoursEnd: TimeSpan.FromHours(7));
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Breaking News");
        var service = CreateService(CreateTimeProviderAt(hour));

        await service.NotifyNewItemsAsync(feed, [item]);

        Assert.Empty(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that notifications are sent while the local time is outside quiet hours.
    /// </summary>
    /// <param name="hour">The local hour of the day to test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(12)]
    [InlineData(21)]
    public async Task NotifyNewItemsAsync_OutsideQuietHours_Sends(int hour)
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, quietHoursStart: TimeSpan.FromHours(22), quietHoursEnd: TimeSpan.FromHours(7));
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Breaking News");
        var service = CreateService(CreateTimeProviderAt(hour));

        await service.NotifyNewItemsAsync(feed, [item]);

        Assert.Single(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that identical quiet-hours bounds form an empty interval and do not suppress notifications.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_QuietHoursStartEqualsEnd_Sends()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, quietHoursStart: TimeSpan.FromHours(12), quietHoursEnd: TimeSpan.FromHours(12));
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Breaking News");
        var service = CreateService(CreateTimeProviderAt(12));

        await service.NotifyNewItemsAsync(feed, [item]);

        Assert.Single(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that a single quiet-hours bound (other side <see langword="null"/>) disables quiet hours entirely.
    /// </summary>
    /// <param name="start">The quiet-hours start to persist.</param>
    /// <param name="end">The quiet-hours end to persist.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData("22:00", null)]
    [InlineData(null, "07:00")]
    public async Task NotifyNewItemsAsync_OnlyOneQuietHoursBound_Sends(string? start, string? end)
    {
        await TestSettingsHelper.SaveAsync(
            _settingsRepository,
            quietHoursStart: start is null ? null : TimeSpan.Parse(start),
            quietHoursEnd: end is null ? null : TimeSpan.Parse(end));
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Breaking News");
        var service = CreateService(CreateTimeProviderAt(23));

        await service.NotifyNewItemsAsync(feed, [item]);

        Assert.Single(_localNotifications.ShownNotifications);
    }

    /// <summary>
    /// Verifies that a keyword match in the title or HTML content excludes the item from notifications.
    /// </summary>
    /// <param name="contentHtml">The HTML content of the item, or <see langword="null"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("<p>Read more about sport</p>")]
    public async Task NotifyNewItemsAsync_KeywordMatch_SkipsItem(string? contentHtml)
    {
        await _keywordRepository.AddAsync(new Keyword
        {
            Id = Guid.NewGuid(),
            KeywordText = "sport",
        });
        var feed = CreateFeed();
        var matchedItem = CreateItem(feed.Id, contentHtml is null ? "Latest sport results" : "News", contentHtml);
        var normalItem = CreateItem(feed.Id, "Technology update");

        await CreateService().NotifyNewItemsAsync(feed, [matchedItem, normalItem]);

        var notification = Assert.Single(_localNotifications.ShownNotifications);
        Assert.Equal(normalItem.Id.ToString(), notification.Identifier);
        Assert.Equal("Technology update", notification.Body);
    }

    /// <summary>
    /// Verifies that the default individual mode sends one notification per item with the item identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier()
    {
        var feed = CreateFeed();
        var itemOne = CreateItem(feed.Id, "First item");
        var itemTwo = CreateItem(feed.Id, "Second item");

        await CreateService().NotifyNewItemsAsync(feed, [itemOne, itemTwo]);

        Assert.Equal(2, _localNotifications.ShownNotifications.Count);
        Assert.Contains(_localNotifications.ShownNotifications,
            n => n.Identifier == itemOne.Id.ToString() && n.Title == feed.Title && n.Body == "First item");
        Assert.Contains(_localNotifications.ShownNotifications,
            n => n.Identifier == itemTwo.Id.ToString() && n.Title == feed.Title && n.Body == "Second item");
        Assert.All(_localNotifications.ShownNotifications,
            n => Assert.Equal(n.Identifier, n.UserInfo!["itemId"]));
    }

    /// <summary>
    /// Verifies that the individual mode forwards the item link in the user info for tap handling.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_SendsItemLink_InUserInfo()
    {
        var feed = CreateFeed();
        var item = CreateItem(feed.Id, "Linked item", link: "https://example.com/article");

        await CreateService().NotifyNewItemsAsync(feed, [item]);

        var notification = Assert.Single(_localNotifications.ShownNotifications);
        Assert.NotNull(notification.UserInfo);
        Assert.Equal(item.Id.ToString(), notification.UserInfo["itemId"]);
        Assert.Equal("https://example.com/article", notification.UserInfo["link"]);
    }

    /// <summary>
    /// Verifies that the summary mode sends a single notification with the item count and a stable identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_SummaryEnabled_SendsSingleSummary()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);
        var feed = CreateFeed();
        var itemOne = CreateItem(feed.Id, "First item");
        var itemTwo = CreateItem(feed.Id, "Second item");

        await CreateService().NotifyNewItemsAsync(feed, [itemOne, itemTwo]);

        var notification = Assert.Single(_localNotifications.ShownNotifications);
        Assert.Equal(feed.Title, notification.Title);
        Assert.Contains("2", notification.Body);
        Assert.StartsWith($"{feed.Id}-", notification.Identifier);
        Assert.NotNull(notification.UserInfo);
        Assert.Equal(feed.Id.ToString(), notification.UserInfo["feedId"]);
    }

    /// <summary>
    /// Verifies that the summary identifier is stable for the same set of items
    /// and changes when the item set changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);
        var feed = CreateFeed();
        var items = new[] { CreateItem(feed.Id, "First item"), CreateItem(feed.Id, "Second item") };
        var service = CreateService();

        await service.NotifyNewItemsAsync(feed, items);
        var firstIdentifier = Assert.Single(_localNotifications.ShownNotifications).Identifier;
        _localNotifications.ShownNotifications.Clear();
        await service.NotifyNewItemsAsync(feed, items);
        var secondIdentifier = Assert.Single(_localNotifications.ShownNotifications).Identifier;
        _localNotifications.ShownNotifications.Clear();
        var changedItems = new[] { items[0], CreateItem(feed.Id, "Third item") };
        await service.NotifyNewItemsAsync(feed, changedItems);
        var changedIdentifier = Assert.Single(_localNotifications.ShownNotifications).Identifier;

        Assert.Equal(firstIdentifier, secondIdentifier);
        Assert.NotEqual(firstIdentifier, changedIdentifier);
    }

    /// <summary>
    /// Verifies that keyword-filtered items are excluded from the summary count and identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);
        await _keywordRepository.AddAsync(new Keyword
        {
            Id = Guid.NewGuid(),
            KeywordText = "sport",
        });
        var feed = CreateFeed();
        var matchedItem = CreateItem(feed.Id, "Latest sport results");
        var normalItem = CreateItem(feed.Id, "Technology update");

        await CreateService().NotifyNewItemsAsync(feed, [matchedItem, normalItem]);

        var notification = Assert.Single(_localNotifications.ShownNotifications);
        Assert.Contains("1", notification.Body);
        Assert.Contains("Technology update", notification.Body);
        Assert.DoesNotContain("sport", notification.Body);
        Assert.StartsWith($"{feed.Id}-", notification.Identifier);
    }

    private NotificationService CreateService(TimeProvider? timeProvider = null)
    {
        return new NotificationService(_settingsRepository, _keywordFilter, _localNotifications, timeProvider);
    }

    private static FakeTimeProvider CreateTimeProviderAt(int hour)
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 11, hour, 0, 0, TimeSpan.Zero));
        provider.SetLocalTimeZone(TimeZoneInfo.Utc);
        return provider;
    }

    private static Feed CreateFeed(bool notificationsEnabled = true)
    {
        return new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/rss",
            Title = "Test Feed",
            NotificationsEnabled = notificationsEnabled,
        };
    }

    private static Item CreateItem(Guid feedId, string title, string? contentHtml = null, string? link = null)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = title,
            Link = link,
            GuidOrHash = Guid.NewGuid().ToString("N"),
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = contentHtml,
        };
    }
}
