// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains integration tests for the <see cref="FeedDetailViewModel"/> class.
/// </summary>
public class FeedDetailViewModelTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly FeedRepository _feedRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly FakeFeedSyncService _syncService;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly FakeLocalNotificationService _localNotificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedDetailViewModelTests"/> class.
    /// </summary>
    public FeedDetailViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory, new FakeItemContentStore());
        _feedRepository = new FeedRepository(_factory, new FakeItemContentStore());
        _categoryRepository = new CategoryRepository(_factory);
        _syncService = new FakeFeedSyncService();
        _networkStatusService = new FakeNetworkStatusService();
        _localNotificationService = new FakeLocalNotificationService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private FeedDetailViewModel CreateViewModel(
        IItemRepository? itemRepository = null,
        IFeedRepository? feedRepository = null,
        ICategoryRepository? categoryRepository = null)
    {
        return new FeedDetailViewModel(
            itemRepository ?? _itemRepository,
            feedRepository ?? _feedRepository,
            _syncService,
            categoryRepository ?? _categoryRepository,
            _networkStatusService,
            _localNotificationService);
    }

    private async Task<Guid> SeedFeedAsync(string title = "Test Feed", string url = "https://example.com/rss", bool notificationsEnabled = true)
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = title,
            NotificationsEnabled = notificationsEnabled,
        });
        return feedId;
    }

    private async Task SeedItemAsync(Guid feedId, string title, DateTime publishedAt, bool isRead = false)
    {
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = title,
            GuidOrHash = Guid.NewGuid().ToString("N"),
            IsRead = isRead,
            IsSavedForLater = false,
            PublishedAt = publishedAt,
        });
    }

    /// <summary>
    /// Verifies that LoadAsync loads the feed header data and the first page of
    /// articles including read and unread items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_PopulatesFeedAndItems()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var feedId = await SeedFeedAsync("Detail Feed");
        await _feedRepository.UpdateAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Detail Feed",
            CategoryId = categoryId,
            NotificationsEnabled = true,
        });
        await SeedItemAsync(feedId, "Unread Item", new DateTime(2026, 1, 2), isRead: false);
        await SeedItemAsync(feedId, "Read Item", new DateTime(2026, 1, 1), isRead: true);
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(feedId);

        Assert.True(viewModel.HasFeed);
        Assert.Equal("Detail Feed", viewModel.Feed?.Title);
        Assert.Equal("Tech", viewModel.Feed?.CategoryName);
        Assert.Equal(1, viewModel.Feed?.UnreadCount);
        Assert.Equal(2, viewModel.Items.Count);
        Assert.False(viewModel.HasError);
        Assert.Equal("Unread Item", viewModel.Items[0].Title);
        Assert.Equal("Read Item", viewModel.Items[1].Title);
    }

    /// <summary>
    /// Verifies that the article list is ordered by PublishedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_OrdersByPublishedAtDescending()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Older", new DateTime(2026, 1, 1));
        await SeedItemAsync(feedId, "Newer", new DateTime(2026, 1, 2));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(feedId);

        Assert.Equal("Newer", viewModel.Items[0].Title);
        Assert.Equal("Older", viewModel.Items[1].Title);
    }

    /// <summary>
    /// Verifies that an unknown feed id reports a load error and leaves the
    /// page empty.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_UnknownFeed_SetsErrorMessage()
    {
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasFeed);
        Assert.Null(viewModel.Feed);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.Empty(viewModel.Items);
    }

    /// <summary>
    /// Verifies that the load failure of the article query sets the localized
    /// load error message.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_WhenRepositoryFails_SetsLocalizedErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel(new FailingItemRepository(_itemRepository));

        await viewModel.LoadAsync(feedId);

        Assert.True(viewModel.HasFeed);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.Empty(viewModel.Items);
        Assert.False(viewModel.HasMore);
    }

    /// <summary>
    /// Verifies that a failing feed repository reports the localized load error
    /// instead of letting the exception escape to the page.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_WhenFeedRepositoryFails_SetsLocalizedErrorMessage()
    {
        var viewModel = CreateViewModel(feedRepository: new ThrowingFeedRepository());

        await viewModel.LoadAsync(Guid.NewGuid());

        Assert.False(viewModel.HasFeed);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.Empty(viewModel.Items);
    }

    /// <summary>
    /// Verifies that a failing category repository reports the localized load
    /// error instead of letting the exception escape to the page.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadAsync_WhenCategoryRepositoryFails_SetsLocalizedErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel(categoryRepository: new ThrowingCategoryRepository());

        await viewModel.LoadAsync(feedId);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.Empty(viewModel.Items);
    }

    /// <summary>
    /// Verifies that LoadMoreCommand appends the next page while items remain.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_AppendsNextPage()
    {
        var feedId = await SeedFeedAsync();
        for (var i = 0; i < FeedDetailViewModel.PageSize + 5; i++)
        {
            await SeedItemAsync(feedId, $"Item {i:D2}", new DateTime(2026, 1, 1).AddDays(i));
        }

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        Assert.Equal(FeedDetailViewModel.PageSize, viewModel.Items.Count);
        Assert.True(viewModel.HasMore);

        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(FeedDetailViewModel.PageSize + 5, viewModel.Items.Count);
        Assert.False(viewModel.HasMore);
    }

    /// <summary>
    /// Verifies that LoadMoreCommand does nothing when all items are loaded.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_WhenNoMoreItems_DoesNothing()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Only Item", new DateTime(2026, 1, 1));
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        Assert.False(viewModel.HasMore);

        Assert.False(viewModel.LoadMoreCommand.CanExecute(null));
        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Items);
    }

    /// <summary>
    /// Verifies that changing the search text filters server-side and restarts
    /// the paged list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchText_Changed_ResetsPagingAndFilters()
    {
        var feedId = await SeedFeedAsync();
        for (var i = 0; i < FeedDetailViewModel.PageSize; i++)
        {
            await SeedItemAsync(feedId, $"News {i:D2}", new DateTime(2026, 1, 1).AddDays(i));
        }

        await SeedItemAsync(feedId, "Special Report", new DateTime(2026, 2, 1));
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        Assert.Equal(FeedDetailViewModel.PageSize, viewModel.Items.Count);
        Assert.True(viewModel.HasMore);

        viewModel.SearchText = "Special";

        await TestWaitHelper.WaitUntilAsync(() => viewModel.Items.Count == 1);
        Assert.Equal("Special Report", viewModel.Items[0].Title);
        Assert.False(viewModel.HasMore);

        viewModel.SearchText = string.Empty;

        await TestWaitHelper.WaitUntilAsync(() => viewModel.Items.Count == FeedDetailViewModel.PageSize);
        Assert.True(viewModel.HasMore);
    }

    /// <summary>
    /// Verifies that rapid search text changes are debounced: only the last
    /// change restarts the list and hits the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchText_RapidChanges_DebouncesListRestart()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Special Report", new DateTime(2026, 1, 1));
        await SeedItemAsync(feedId, "Other Item", new DateTime(2026, 1, 2));
        var counting = new CountingItemRepository(_itemRepository);
        var viewModel = CreateViewModel(counting);
        await viewModel.LoadAsync(feedId);
        var callsAfterLoad = counting.GetByFeedCallCount;

        viewModel.SearchText = "S";
        viewModel.SearchText = "Sp";
        viewModel.SearchText = "Special";

        await TestWaitHelper.WaitUntilAsync(() => viewModel.Items.Count == 1);
        Assert.Equal("Special Report", viewModel.Items[0].Title);

        // Any erroneously queued extra restart would have finished within this
        // window, so the repository must have seen exactly one more query.
        await Task.Delay(700);
        Assert.Equal(callsAfterLoad + 1, counting.GetByFeedCallCount);
    }

    /// <summary>
    /// Verifies that the refresh command synchronizes only this feed and
    /// reloads the view state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_InvokesSyncFeed_AndReloads()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(1, _syncService.SyncFeedCallCount);
        Assert.Equal(feedId, _syncService.LastSyncFeedId);
        Assert.Equal(0, _syncService.SyncAllCallCount);
        Assert.True(viewModel.HasFeed);
        Assert.False(viewModel.IsSyncing);
    }

    /// <summary>
    /// Verifies that the refresh command skips the synchronization while
    /// offline without raising an error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenOffline_SkipsSync()
    {
        var feedId = await SeedFeedAsync();
        _networkStatusService.IsOnline = false;
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(0, _syncService.SyncFeedCallCount);
        Assert.False(viewModel.IsSyncing);
        Assert.False(viewModel.HasSyncError);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that a skipped refresh (device offline) leaves the loaded list
    /// untouched instead of restarting the paged query anyway — the same
    /// guard <see cref="FeedsViewModel"/> applies to its post-sync reload.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenOffline_DoesNotReloadList()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Item", new DateTime(2026, 1, 1));
        _networkStatusService.IsOnline = false;
        var counting = new CountingItemRepository(_itemRepository);
        var viewModel = CreateViewModel(counting);
        await viewModel.LoadAsync(feedId);
        var callsAfterLoad = counting.GetByFeedCallCount;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(0, _syncService.SyncFeedCallCount);
        Assert.Equal(callsAfterLoad, counting.GetByFeedCallCount);
        Assert.Single(viewModel.Items);
    }

    /// <summary>
    /// Verifies that a failed feed sync surfaces the localized sync error
    /// instead of the raw technical message.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        _syncService.SyncFeedResult = new SyncResult(FeedHealth.Error, 0, "Network error");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSyncError);
        Assert.Equal(AppResources.SyncStatusError, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    /// <summary>
    /// Verifies that a throwing feed sync surfaces the localized sync error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenSyncThrows_SetsLocalizedSyncErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        _syncService.SyncFeedException = new InvalidOperationException("raw provider message");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSyncError);
        Assert.Equal(AppResources.SyncStatusError, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that an IsSyncing value preset by the RefreshView binding does
    /// not turn the refresh into a no-op.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.IsSyncing = true;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(feedId, _syncService.LastSyncFeedId);
        Assert.False(viewModel.IsSyncing);
    }

    /// <summary>
    /// Verifies that a repository failure while reloading the feed after a
    /// successful sync reports the localized load error instead of letting the
    /// exception fault the command task unobserved.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenReloadThrows_SetsLocalizedErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        var failing = new FailingReloadFeedRepository(_feedRepository);
        var viewModel = CreateViewModel(feedRepository: failing);
        await viewModel.LoadAsync(feedId);
        failing.FailGetAllWithDetails = true;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.False(viewModel.IsSyncing);
    }

    /// <summary>
    /// Verifies that renaming a feed persists the new title and reloads the
    /// header.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_UpdatesTitle()
    {
        var feedId = await SeedFeedAsync("Old Title");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        await viewModel.RenameFeedAsync(feed, "  New Title  ");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("New Title", saved?.Title);
        Assert.False(viewModel.HasError);
        Assert.Equal("New Title", viewModel.Feed?.Title);
    }

    /// <summary>
    /// Verifies that an empty rename title reports the validation error and
    /// keeps the current title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_EmptyTitle_SetsError()
    {
        var feedId = await SeedFeedAsync("Keep Me");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        await viewModel.RenameFeedAsync(feed, "   ");

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedTitleEmpty, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("Keep Me", saved?.Title);
    }

    /// <summary>
    /// Verifies that renaming without a feed does nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_NullFeed_DoesNothing()
    {
        var feedId = await SeedFeedAsync("Keep Me");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.RenameFeedAsync(null, "New Title");

        Assert.False(viewModel.HasError);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("Keep Me", saved?.Title);
    }

    /// <summary>
    /// Verifies that renaming a feed keeps the stored favicon URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_PreservesFaviconUrl()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Old Title",
            NotificationsEnabled = true,
            FaviconUrl = "https://example.com/favicon.ico",
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        await viewModel.RenameFeedAsync(feed, "New Title");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Equal("New Title", saved.Title);
        Assert.Equal("https://example.com/favicon.ico", saved.FaviconUrl);
    }

    /// <summary>
    /// Verifies that renaming a feed keeps the stored sync error fields.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_PreservesLastError()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Old Title",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Error,
            LastErrorKind = FeedSyncErrorKind.HttpStatus,
            LastErrorMessage = "Synchronization failed: 404",
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        await viewModel.RenameFeedAsync(feed, "New Title");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Equal("New Title", saved.Title);
        Assert.Equal(FeedSyncErrorKind.HttpStatus, saved.LastErrorKind);
        Assert.Equal("Synchronization failed: 404", saved.LastErrorMessage);
    }

    /// <summary>
    /// Verifies that changing the category persists the selected category id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ChangeFeedCategoryAsync_SetsCategoryId()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;
        var category = viewModel.Categories.First(c => c.Id == categoryId);

        await viewModel.ChangeFeedCategoryAsync(feed, category);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(categoryId, saved?.CategoryId);
        Assert.Equal("Tech", viewModel.Feed?.CategoryName);
    }

    /// <summary>
    /// Verifies that the pseudo-category entry clears the feed's category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ChangeFeedCategoryAsync_EmptyGuid_ClearsCategory()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var feedId = await SeedFeedAsync();
        await _feedRepository.UpdateAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Test Feed",
            CategoryId = categoryId,
            NotificationsEnabled = true,
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;
        var none = viewModel.Categories.First(c => c.Id == Guid.Empty);

        await viewModel.ChangeFeedCategoryAsync(feed, none);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Null(saved?.CategoryId);
    }

    /// <summary>
    /// Verifies that null arguments abort the category change without touching
    /// the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ChangeFeedCategoryAsync_NullArguments_DoNothing()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.ChangeFeedCategoryAsync(null, null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Null(saved.CategoryId);
    }

    /// <summary>
    /// Verifies that GetFeedErrorMessage maps a stored error kind to the
    /// matching localized <c>FeedErrorKind*</c> text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetFeedErrorMessage_MapsKindToLocalizedText()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Broken",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Error,
            LastErrorKind = FeedSyncErrorKind.Network,
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        var message = viewModel.GetFeedErrorMessage(feed!);

        Assert.Equal(AppResources.FeedErrorKindNetwork, message);
    }

    /// <summary>
    /// Verifies that an empty or unknown error kind falls back to the generic
    /// unknown-error text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetFeedErrorMessage_FallsBackToUnknown()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Broken",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Error,
            LastErrorKind = "NotARealKind",
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        var message = viewModel.GetFeedErrorMessage(feed!);

        Assert.Equal(AppResources.FeedErrorKindUnknown, message);
    }

    /// <summary>
    /// Verifies that the stored technical message is appended to the localized
    /// category text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetFeedErrorMessage_AppendsTechnicalMessage()
    {
        var feedId = Guid.NewGuid();
        await _feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Broken",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Error,
            LastErrorKind = FeedSyncErrorKind.Parse,
            LastErrorMessage = "Synchronization failed: invalid xml",
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var feed = viewModel.Feed;

        var message = viewModel.GetFeedErrorMessage(feed!);

        Assert.StartsWith(AppResources.FeedErrorKindParse, message);
        Assert.Contains("Synchronization failed: invalid xml", message);
    }

    /// <summary>
    /// Verifies that repeated names get a counter suffix so each option label
    /// is unique.
    /// </summary>
    [Fact]
    public void MakeUniqueOptionLabels_DuplicateNames_AppendsCounterSuffix()
    {
        var labels = FeedDetailViewModel.MakeUniqueOptionLabels(["News", "News"]);

        Assert.Equal(["News", "News (2)"], labels);
    }

    /// <summary>
    /// Verifies that a literal name colliding with a generated suffix still
    /// produces unique labels — the action sheet returns the tapped button's
    /// text, so a duplicated label would map the selection to the wrong entry.
    /// </summary>
    [Fact]
    public void MakeUniqueOptionLabels_WhenLiteralNameCollidesWithGeneratedSuffix_KeepsEveryLabelUnique()
    {
        var labels = FeedDetailViewModel.MakeUniqueOptionLabels(["News", "News", "News (2)"]);

        Assert.Equal(3, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("News", labels[0]);
        Assert.Equal("News (2)", labels[1]);
    }

    /// <summary>
    /// Verifies the same collision guarantee when the literal suffix name is
    /// stored before the duplicates that would generate it.
    /// </summary>
    [Fact]
    public void MakeUniqueOptionLabels_WhenLiteralSuffixNameComesFirst_KeepsEveryLabelUnique()
    {
        var labels = FeedDetailViewModel.MakeUniqueOptionLabels(["News (2)", "News", "News"]);

        Assert.Equal(3, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("News (2)", labels[0]);
        Assert.Equal("News", labels[1]);
    }

    /// <summary>
    /// Verifies that the edit command opens the sheet with the feed's current
    /// URL and notification flag loaded into the form.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EditCommand_PrefillsForm()
    {
        var feedId = await SeedFeedAsync("Edit Me", "https://example.com/edit-me", notificationsEnabled: false);
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        viewModel.EditCommand.Execute(null);

        Assert.True(viewModel.ShowEditForm);
        Assert.Equal("https://example.com/edit-me", viewModel.EditUrl);
        Assert.False(viewModel.EditNotificationsEnabled);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that saving the edit sheet persists the URL and notification
    /// flag and closes the sheet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_PersistsUrlAndNotifications()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditUrl = "https://example.com/new-feed";
        viewModel.EditNotificationsEnabled = false;

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/new-feed", saved?.Url);
        Assert.False(saved?.NotificationsEnabled);
        Assert.False(viewModel.ShowEditForm);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that an invalid URL keeps the sheet open and reports the
    /// validation error without updating the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_InvalidUrl_SetsError()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditUrl = "not a url";

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowEditForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedUrlInvalid, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/rss", saved?.Url);
    }

    /// <summary>
    /// Verifies that a duplicate URL keeps the sheet open, reports the
    /// duplicate error and does not update the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_DuplicateUrl_SetsError()
    {
        var feedId = await SeedFeedAsync("Feed A", "https://example.com/feed-a");
        await SeedFeedAsync("Feed B", "https://example.com/feed-b");
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditUrl = "https://example.com/feed-b";

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowEditForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/feed-a", saved?.Url);
    }

    /// <summary>
    /// Verifies that saving the edit sheet keeps the stored category
    /// assignment — the sheet offers no category picker, so the stored value
    /// is the only source of truth.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_PreservesCategoryId()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var feedId = await SeedFeedAsync();
        await _feedRepository.UpdateAsync(new Feed
        {
            Id = feedId,
            Url = "https://example.com/rss",
            Title = "Test Feed",
            CategoryId = categoryId,
            NotificationsEnabled = true,
        });
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditUrl = "https://example.com/new-feed";

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(categoryId, saved?.CategoryId);
    }

    /// <summary>
    /// Verifies that saving without a loaded feed does nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_WithoutFeed_DoesNothing()
    {
        var viewModel = CreateViewModel();
        viewModel.EditUrl = "https://example.com/new-feed";

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        Assert.Empty(await _feedRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that a successful save closes the sheet and resets the edit
    /// state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_ResetsEditState()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditNotificationsEnabled = false;

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        Assert.False(viewModel.ShowEditForm);
        Assert.Equal(string.Empty, viewModel.EditUrl);
        Assert.True(viewModel.EditNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a repository failure while saving keeps the sheet open and
    /// reports the localized action error instead of letting the exception
    /// fault the command task unobserved.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveEditCommand_WhenRepositoryThrows_SetsErrorAndKeepsSheetOpen()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel(feedRepository: new FailingSaveFeedRepository(_feedRepository));
        await viewModel.LoadAsync(feedId);
        viewModel.EditCommand.Execute(null);
        viewModel.EditUrl = "https://example.com/new-feed";

        await viewModel.SaveEditCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowEditForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorActionFailed, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/rss", saved?.Url);
    }

    /// <summary>
    /// Verifies that deleting the feed removes it from the repository.
    /// Navigation back to the feed list is covered by the E2E tests.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteFeedAsync_RemovesFeed()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);

        await viewModel.DeleteFeedAsync();

        Assert.Null(await _feedRepository.GetByIdAsync(feedId));
    }

    /// <summary>
    /// Verifies that deleting without a loaded feed does nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteFeedAsync_WithoutFeed_DoesNothing()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();

        await viewModel.DeleteFeedAsync();

        Assert.NotNull(await _feedRepository.GetByIdAsync(feedId));
    }

    /// <summary>
    /// Verifies that MarkReadCommand marks the item as read and keeps it in
    /// the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_MarksItemRead()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Item", new DateTime(2026, 1, 1));
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var item = viewModel.Items[0];

        await viewModel.MarkReadCommand.ExecuteAsync(item);

        Assert.Single(viewModel.Items);
        Assert.True(viewModel.Items[0].IsRead);
        var saved = await _itemRepository.GetByIdAsync(item.Id);
        Assert.True(saved?.IsRead);
    }

    /// <summary>
    /// Verifies that ToggleSavedCommand flips the saved flag and keeps the
    /// item in the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_TogglesFlag_KeepsItemInList()
    {
        var feedId = await SeedFeedAsync();
        await SeedItemAsync(feedId, "Item", new DateTime(2026, 1, 1));
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        var item = viewModel.Items[0];

        await viewModel.ToggleSavedCommand.ExecuteAsync(item);

        Assert.Single(viewModel.Items);
        Assert.True(viewModel.Items[0].IsSavedForLater);
        var saved = await _itemRepository.GetByIdAsync(item.Id);
        Assert.True(saved?.IsSavedForLater);
    }

    /// <summary>
    /// Verifies that connectivity changes update the IsOnline state while the
    /// page is attached.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ConnectivityChanged_UpdatesIsOnline()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(feedId);
        viewModel.AttachConnectivity();
        Assert.True(viewModel.IsOnline);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.False(viewModel.IsOnline);
    }

    /// <summary>
    /// A <see cref="DelegatingItemRepository"/> that fails the paged feed query
    /// to simulate a load failure.
    /// </summary>
    private sealed class FailingItemRepository : DelegatingItemRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailingItemRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public FailingItemRepository(IItemRepository inner)
            : base(inner)
        {
        }

        /// <inheritdoc />
        public override Task<IReadOnlyList<ItemListItem>> GetByFeedAsync(
            Guid feedId,
            int page,
            int pageSize,
            string? searchTerm = null)
        {
            return Task.FromException<IReadOnlyList<ItemListItem>>(new InvalidOperationException("Simulated load failure."));
        }
    }

    /// <summary>
    /// A <see cref="DelegatingFeedRepository"/> that fails the feed-detail
    /// query once <see cref="FailGetAllWithDetails"/> is set, to simulate a
    /// repository failure on reload after a successful initial load.
    /// </summary>
    private sealed class FailingReloadFeedRepository : DelegatingFeedRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailingReloadFeedRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public FailingReloadFeedRepository(IFeedRepository inner)
            : base(inner)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether <c>GetAllWithDetailsAsync</c> fails.
        /// </summary>
        public bool FailGetAllWithDetails { get; set; }

        /// <inheritdoc />
        public override Task<IReadOnlyList<FeedListItem>> GetAllWithDetailsAsync()
        {
            return FailGetAllWithDetails
                ? Task.FromException<IReadOnlyList<FeedListItem>>(new InvalidOperationException("Simulated reload failure."))
                : base.GetAllWithDetailsAsync();
        }
    }

    /// <summary>
    /// A <see cref="DelegatingFeedRepository"/> that fails the URL lookup used
    /// by the edit-sheet save path.
    /// </summary>
    private sealed class FailingSaveFeedRepository : DelegatingFeedRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailingSaveFeedRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public FailingSaveFeedRepository(IFeedRepository inner)
            : base(inner)
        {
        }

        /// <inheritdoc />
        public override Task<Feed?> GetByUrlAsync(string url)
        {
            return Task.FromException<Feed?>(new InvalidOperationException("Simulated save failure."));
        }
    }

    /// <summary>
    /// A <see cref="DelegatingItemRepository"/> that counts the paged feed
    /// queries to observe list restarts.
    /// </summary>
    private sealed class CountingItemRepository : DelegatingItemRepository
    {
        private int _getByFeedCallCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountingItemRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public CountingItemRepository(IItemRepository inner)
            : base(inner)
        {
        }

        /// <summary>
        /// Gets the number of paged <c>GetByFeedAsync</c> calls so far.
        /// </summary>
        public int GetByFeedCallCount => _getByFeedCallCount;

        /// <inheritdoc />
        public override async Task<IReadOnlyList<ItemListItem>> GetByFeedAsync(
            Guid feedId,
            int page,
            int pageSize,
            string? searchTerm = null)
        {
            _getByFeedCallCount++;
            return await base.GetByFeedAsync(feedId, page, pageSize, searchTerm);
        }
    }
}
