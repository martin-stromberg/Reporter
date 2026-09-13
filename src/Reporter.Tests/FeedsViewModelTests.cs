// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains integration tests for the <see cref="FeedsViewModel"/> class.
/// </summary>
public class FeedsViewModelTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FeedRepository _feedRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly FakeFeedSyncService _syncService;
    private readonly FakeFeedSearchService _searchService;
    private readonly FakeNetworkStatusService _networkStatusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModelTests"/> class.
    /// </summary>
    public FeedsViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _feedRepository = new FeedRepository(_factory);
        _categoryRepository = new CategoryRepository(_factory);
        _syncService = new FakeFeedSyncService();
        _searchService = new FakeFeedSearchService();
        _networkStatusService = new FakeNetworkStatusService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private FeedsViewModel CreateViewModel()
    {
        return new FeedsViewModel(_feedRepository, _categoryRepository, _syncService, _searchService, _networkStatusService);
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

    /// <summary>
    /// Verifies that the refresh command invokes the sync service for the selected feed and reloads the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_InvokesSyncService_AndReloadsList()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.Equal(feedId, _syncService.LastFeedId);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that the refresh all command invokes the sync service and reloads the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshAllCommand_InvokesSyncService_AndReloadsList()
    {
        await SeedFeedAsync("Feed A", "https://example.com/feed-a");
        await SeedFeedAsync("Feed B", "https://example.com/feed-b");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.RefreshAllCommand.ExecuteAsync(null);

        Assert.True(_syncService.SyncAllCalled);
        Assert.Equal(2, viewModel.Feeds.Count);
    }

    /// <summary>
    /// Verifies that an error result from the sync service is surfaced via the
    /// sync error channel as a localized generic message — the raw technical
    /// result text stays in the SyncLog and is not shown in the UI.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        _syncService.NextResult = new SyncResult(FeedHealth.Error, 0, "Network error");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.True(viewModel.HasSyncError);
        Assert.Equal(AppResources.SyncStatusError, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    /// <summary>
    /// Verifies that RefreshAllCommand skips the sync while offline without raising a
    /// separate error message — the persistent offline banner already communicates the state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshAllCommand_WhenOffline_SkipsSyncWithoutError()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        await viewModel.RefreshAllCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsOnline);
        Assert.False(_syncService.SyncAllCalled);
        Assert.Equal(string.Empty, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasSyncError);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that a pull-to-refresh gesture still syncs when the RefreshView TwoWay
    /// binding has already set IsSyncing before the command executes — the reentrancy
    /// guard must not rely on the bound property.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshAllCommand_WhenIsSyncingPresetByBinding_StillSyncs()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.IsSyncing = true;

        await viewModel.RefreshAllCommand.ExecuteAsync(null);

        Assert.True(_syncService.SyncAllCalled);
        Assert.False(viewModel.IsSyncing);
    }

    /// <summary>
    /// Verifies that the single-feed sync also runs when IsSyncing was preset by the
    /// RefreshView TwoWay binding.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        viewModel.IsSyncing = true;

        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.Equal(feedId, _syncService.LastFeedId);
        Assert.False(viewModel.IsSyncing);
    }

    /// <summary>
    /// Verifies that the offline early return of RefreshAllCommand resets an IsSyncing
    /// value preset by the binding, so the refresh indicator stops.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshAllCommand_WhenOffline_ResetsPresetIsSyncing()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        viewModel.IsSyncing = true;

        await viewModel.RefreshAllCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsSyncing);
        Assert.False(_syncService.SyncAllCalled);
        Assert.False(viewModel.HasSyncError);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that the offline early return of RefreshCommand resets an IsSyncing
    /// value preset by the binding, so the refresh indicator stops.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenOffline_ResetsPresetIsSyncing()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        viewModel.IsSyncing = true;

        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.False(viewModel.IsSyncing);
        Assert.Null(_syncService.LastFeedId);
        Assert.False(viewModel.HasSyncError);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that RefreshCommand skips the single-feed sync while offline without
    /// raising a separate error message.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenOffline_SkipsSyncWithoutError()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.False(viewModel.IsOnline);
        Assert.Null(_syncService.LastFeedId);
        Assert.Equal(string.Empty, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasSyncError);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that a thrown sync exception surfaces a localized generic message
    /// instead of the raw exception text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError()
    {
        _syncService.NextException = new InvalidOperationException("raw provider message");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.RefreshAllCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSyncError);
        Assert.Equal(AppResources.SyncStatusError, viewModel.SyncErrorMessage);
    }

    /// <summary>
    /// Verifies that a connectivity status change clears a previously shown sync error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ConnectivityChanged_ClearsSyncErrorMessage()
    {
        _syncService.NextResult = new SyncResult(FeedHealth.Error, 0, "Network error");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.RefreshAllCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasSyncError);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.Equal(string.Empty, viewModel.SyncErrorMessage);
        Assert.False(viewModel.HasSyncError);
    }

    /// <summary>
    /// Verifies that the IsOnline property follows connectivity change events.
    /// </summary>
    [Fact]
    public void ConnectivityChanged_UpdatesIsOnline()
    {
        var viewModel = CreateViewModel();
        Assert.True(viewModel.IsOnline);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.False(viewModel.IsOnline);

        _networkStatusService.IsOnline = true;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.True(viewModel.IsOnline);
    }

    /// <summary>
    /// Verifies that editing a feed loads its notifications flag into the form.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EditCommand_PopulatesFeedNotificationsEnabled()
    {
        var feedId = await SeedFeedAsync(notificationsEnabled: false);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        await viewModel.EditCommand.ExecuteAsync(feed);

        Assert.False(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a new feed persists the notifications flag from the form.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_NewFeed_PersistsNotificationsEnabledFalse()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/new-feed";
        viewModel.NewTitle = "New Feed";
        viewModel.FeedNotificationsEnabled = false;

        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/new-feed");
        Assert.NotNull(saved);
        Assert.False(saved.NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that updating an existing feed persists the notifications flag from the form.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_ExistingFeed_PersistsNotificationsEnabled()
    {
        var feedId = await SeedFeedAsync(notificationsEnabled: true);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Feeds.First(f => f.Id == feedId));

        viewModel.FeedNotificationsEnabled = false;
        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.False(saved.NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a successful save resets the notifications flag to its default.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_ResetsFeedNotificationsEnabled()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/new-feed";
        viewModel.NewTitle = "New Feed";
        viewModel.FeedNotificationsEnabled = false;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a newly saved feed is stored with the <see cref="FeedHealth.Ok"/>
    /// status. The reviewed finding was a consistency issue (string literal vs.
    /// constant with identical value), so this test could not fail beforehand — it
    /// guards the persisted contract.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_NewFeed_PersistsHealthStatusOk()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/new-feed";
        viewModel.NewTitle = "New Feed";

        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/new-feed");
        Assert.NotNull(saved);
        Assert.Equal(FeedHealth.Ok, saved.HealthStatus);
    }

    /// <summary>
    /// Verifies that deleting the feed currently edited resets the notifications flag to its default.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteCommand_ResetsFeedNotificationsEnabled()
    {
        var feedId = await SeedFeedAsync(notificationsEnabled: false);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.EditCommand.ExecuteAsync(feed);
        Assert.False(viewModel.FeedNotificationsEnabled);

        await viewModel.DeleteCommand.ExecuteAsync(feed);

        Assert.True(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that the search command populates the results list and shows it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_PopulatesSearchResults()
    {
        _searchService.NextResults =
        [
            new FeedSearchResult { FeedUrl = "https://example.com/rss", Title = "Example Feed", MatchKind = FeedSearchMatchKind.Directory },
        ];
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "  https://example.com  ";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com", _searchService.LastQuery);
        Assert.Single(viewModel.SearchResults);
        Assert.Equal("https://example.com/rss", viewModel.SearchResults[0].FeedUrl);
        Assert.True(viewModel.ShowSearchResults);
        Assert.False(viewModel.IsSearching);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that a domain-like input is normalized to an https URL before searching.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_DomainInput_NormalizesToHttpsUrl()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "example.com";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com", _searchService.LastQuery);
    }

    /// <summary>
    /// Verifies that free text never reaches the search service and directly shows
    /// the empty results view without offering the direct-add dialog.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults()
    {
        var confirmCalled = false;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ =>
        {
            confirmCalled = true;
            return Task.FromResult(false);
        };
        viewModel.NewUrl = "daily tech news";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(0, _searchService.CallCount);
        Assert.True(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
        Assert.False(confirmCalled);
    }

    /// <summary>
    /// Verifies that an empty input aborts the search without calling the service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenEmpty_DoesNotCallService()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "   ";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(0, _searchService.CallCount);
        Assert.False(viewModel.ShowSearchResults);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that the search is skipped while offline without raising a separate
    /// error message — the persistent offline hint label already communicates the state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenOffline_SkipsSearchWithoutError()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        viewModel.NewUrl = "https://example.com/rss";

        Assert.False(viewModel.SearchCommand.CanExecute(null));

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(0, _searchService.CallCount);
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that an unavailable search sets the search error channel without
    /// touching the form error channel.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage()
    {
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/rss";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSearchError);
        Assert.Equal(AppResources.FeedSearchUnavailable, viewModel.SearchErrorMessage);
        Assert.False(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    /// <summary>
    /// Verifies that an unavailable search for a domain input shows the retry hint
    /// without promising the direct-add path that only exists for full URLs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenServiceUnavailableAndDomainInput_SetsRetryHint()
    {
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "example.com";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSearchError);
        Assert.Equal(AppResources.FeedSearchUnavailableRetry, viewModel.SearchErrorMessage);
        Assert.False(viewModel.ShowSearchResults);
    }

    /// <summary>
    /// Verifies that a failing direct-add dialog does not trigger a second dialog
    /// invocation and does not fault the search command.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce()
    {
        var invocations = 0;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ =>
        {
            invocations++;
            return Task.FromException<bool>(new InvalidOperationException("dialog failed"));
        };
        viewModel.NewUrl = "https://example.com/feed";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(1, invocations);
    }

    /// <summary>
    /// Verifies that an empty result for a valid URL input offers the direct-add
    /// confirmation and prefills the title from the URL host when confirmed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        string? invokedUrl = null;
        viewModel.ConfirmDirectAddAsync = url =>
        {
            invokedUrl = url;
            return Task.FromResult(true);
        };
        viewModel.NewUrl = "https://example.com/feed";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com/feed", invokedUrl);
        Assert.Equal("https://example.com/feed", viewModel.NewUrl);
        Assert.Equal("example.com", viewModel.NewTitle);
        Assert.False(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
    }

    /// <summary>
    /// Verifies that declining the direct-add confirmation keeps the form state
    /// untouched and hides the search results view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ => Task.FromResult(false);
        viewModel.NewUrl = "https://other.example.com/feed";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("https://other.example.com/feed", viewModel.NewUrl);
        Assert.Equal(string.Empty, viewModel.NewTitle);
        Assert.False(viewModel.ShowSearchResults);
    }

    /// <summary>
    /// Verifies that the close command leaves the search results view and clears
    /// the results so the feed list becomes visible again.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CloseSearchResultsCommand_ExitsResultsView()
    {
        _searchService.NextResults =
        [
            new FeedSearchResult { FeedUrl = "https://example.com/rss", MatchKind = FeedSearchMatchKind.Directory },
        ];
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com";
        await viewModel.SearchCommand.ExecuteAsync(null);
        Assert.True(viewModel.ShowSearchResults);
        Assert.Single(viewModel.SearchResults);

        viewModel.CloseSearchResultsCommand.Execute(null);

        Assert.False(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
    }

    /// <summary>
    /// Verifies that a domain input without results does not offer the direct-add
    /// dialog — a domain is not a storable feed URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm()
    {
        var confirmCalled = false;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ =>
        {
            confirmCalled = true;
            return Task.FromResult(true);
        };
        viewModel.NewUrl = "example.com";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(1, _searchService.CallCount);
        Assert.False(confirmCalled);
        Assert.True(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
    }

    /// <summary>
    /// Verifies that changing the URL input clears the search results and hides the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NewUrl_Changed_ClearsSearchState()
    {
        _searchService.NextResults =
        [
            new FeedSearchResult { FeedUrl = "https://example.com/rss", MatchKind = FeedSearchMatchKind.Directory },
        ];
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com";
        await viewModel.SearchCommand.ExecuteAsync(null);
        Assert.True(viewModel.ShowSearchResults);
        Assert.Single(viewModel.SearchResults);

        viewModel.NewUrl = "https://changed.example.com";

        Assert.Empty(viewModel.SearchResults);
        Assert.False(viewModel.ShowSearchResults);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that results of a search are discarded when the user changed the URL
    /// while the search was still in flight — the late results belong to the
    /// abandoned query and must not resurface in the results view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenUrlChangesDuringSearch_DiscardsStaleResults()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<FeedSearchResult>>();
        _searchService.PendingResult = pending;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com";

        var searchTask = viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://other.example.com";
        pending.SetResult(
        [
            new FeedSearchResult { FeedUrl = "https://example.com/rss", MatchKind = FeedSearchMatchKind.Directory },
        ]);
        await searchTask;

        Assert.Empty(viewModel.SearchResults);
        Assert.False(viewModel.ShowSearchResults);
        Assert.False(viewModel.IsSearching);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that subscribing a result persists a feed built from the result and
    /// the form state, then resets the search view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_PersistsFeedFromResult()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedCategory = viewModel.Categories.First(c => c.Id == categoryId);
        viewModel.FeedNotificationsEnabled = false;
        var result = new FeedSearchResult
        {
            FeedUrl = "https://example.com/rss",
            Title = "Example Feed",
            MatchKind = FeedSearchMatchKind.Directory,
        };
        viewModel.SearchResults.Add(result);
        viewModel.ShowSearchResults = true;

        await viewModel.SubscribeResultCommand.ExecuteAsync(result);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/rss");
        Assert.NotNull(saved);
        Assert.Equal("Example Feed", saved.Title);
        Assert.Equal(categoryId, saved.CategoryId);
        Assert.False(saved.NotificationsEnabled);
        Assert.Equal(FeedHealth.Ok, saved.HealthStatus);
        Assert.Empty(viewModel.SearchResults);
        Assert.False(viewModel.ShowSearchResults);
        Assert.Single(viewModel.Feeds);
        Assert.True(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a result without a title stores the feed URL as placeholder title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var result = new FeedSearchResult
        {
            FeedUrl = "https://example.com/rss",
            MatchKind = FeedSearchMatchKind.Discovered,
        };

        await viewModel.SubscribeResultCommand.ExecuteAsync(result);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/rss");
        Assert.NotNull(saved);
        Assert.Equal("https://example.com/rss", saved.Title);
    }

    /// <summary>
    /// Verifies that subscribing a result whose URL is already stored raises the
    /// duplicate error and does not add a second feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd()
    {
        await SeedFeedAsync("Existing", "https://example.com/rss");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var result = new FeedSearchResult
        {
            FeedUrl = "https://example.com/rss",
            Title = "Duplicate",
            MatchKind = FeedSearchMatchKind.Directory,
        };
        viewModel.SearchResults.Add(result);
        viewModel.ShowSearchResults = true;

        await viewModel.SubscribeResultCommand.ExecuteAsync(result);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        Assert.Single(viewModel.Feeds);
        Assert.True(viewModel.ShowSearchResults);
    }

    /// <summary>
    /// Verifies that connectivity changes update the search command's CanExecute
    /// state and clear the search error channel.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ConnectivityChanged_UpdatesSearchCommandCanExecute()
    {
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.True(viewModel.SearchCommand.CanExecute(null));

        viewModel.NewUrl = "https://example.com/rss";
        await viewModel.SearchCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasSearchError);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.False(viewModel.SearchCommand.CanExecute(null));
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
        Assert.False(viewModel.HasSearchError);

        _networkStatusService.IsOnline = true;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.True(viewModel.SearchCommand.CanExecute(null));
    }

    private sealed class FakeFeedSyncService : IFeedSyncService
    {
        /// <summary>
        /// Gets the identifier of the feed passed to the last <see cref="SyncFeedAsync"/> call.
        /// </summary>
        public Guid? LastFeedId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="SyncAllAsync"/> was called.
        /// </summary>
        public bool SyncAllCalled { get; private set; }

        /// <summary>
        /// Gets or sets the result returned by the fake service.
        /// </summary>
        /// <returns>The <see cref="SyncResult"/> used by the fake service.</returns>
        public SyncResult NextResult { get; set; } = new SyncResult(FeedHealth.Ok, 0);

        /// <summary>
        /// Gets or sets the exception thrown by the fake service, if any.
        /// </summary>
        public Exception? NextException { get; set; }

        /// <inheritdoc />
        public Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)
        {
            LastFeedId = feedId;
            return NextException is not null
                ? Task.FromException<SyncResult>(NextException)
                : Task.FromResult(NextResult);
        }

        /// <inheritdoc />
        public Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
        {
            SyncAllCalled = true;
            return NextException is not null
                ? Task.FromException<SyncResult>(NextException)
                : Task.FromResult(NextResult);
        }
    }
}
