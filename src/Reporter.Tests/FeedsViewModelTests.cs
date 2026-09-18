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
    private readonly FakeFeedIconService _feedIconService;
    private readonly FakeNetworkStatusService _networkStatusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModelTests"/> class.
    /// </summary>
    public FeedsViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _feedRepository = new FeedRepository(_factory, new FakeItemContentStore());
        _categoryRepository = new CategoryRepository(_factory);
        _syncService = new FakeFeedSyncService();
        _searchService = new FakeFeedSearchService();
        _feedIconService = new FakeFeedIconService();
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
        return new FeedsViewModel(_feedRepository, _categoryRepository, _syncService, _searchService, _feedIconService, _networkStatusService);
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
    /// Verifies that the direct-add command persists the feed with the file-name
    /// fallback title and the fixed defaults without calling the search service —
    /// it stays available while offline.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_WhenOffline_PersistsFeedWithDefaults()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://heise.de/rss/heise-atom.xml";

        Assert.False(viewModel.SearchCommand.CanExecute(null));
        Assert.True(viewModel.DirectAddCommand.CanExecute(null));

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://heise.de/rss/heise-atom.xml");
        Assert.NotNull(saved);
        Assert.Equal("heise-atom.xml", saved.Title);
        Assert.Null(saved.CategoryId);
        Assert.True(saved.NotificationsEnabled);
        Assert.Equal(FeedHealth.Ok, saved.HealthStatus);
        Assert.Equal(0, _searchService.CallCount);
        Assert.False(viewModel.ShowAddForm);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that a domain-like input is normalized to an https URL before it
    /// is persisted directly.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_DomainInput_NormalizesToHttpsUrl()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "example.com/feed.xml";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/feed.xml");
        Assert.NotNull(saved);
        Assert.Equal("feed.xml", saved.Title);
    }

    /// <summary>
    /// Verifies that an invalid direct-add input surfaces the URL error inside
    /// the still-open sheet without persisting or searching.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "not a url";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedUrlInvalid, viewModel.ErrorMessage);
        Assert.True(viewModel.ShowAddForm);
        Assert.Equal(0, _searchService.CallCount);
        Assert.Empty(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that a direct add of an already stored URL surfaces the duplicate
    /// error inside the still-open sheet without persisting a second feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_WhenDuplicate_SetsErrorAndKeepsSheetOpen()
    {
        await SeedFeedAsync("Existing", "https://example.com/feed");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com/feed";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        Assert.True(viewModel.ShowAddForm);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that a rejected direct add leaves the duplicate error as the
    /// only visible message in the sheet — a stale search error from a failed
    /// search must not stay next to it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_WhenDuplicate_ClearsStaleSearchError()
    {
        await SeedFeedAsync("Existing", "https://example.com/feed");
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com/feed";
        await viewModel.SearchCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasSearchError);

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        Assert.False(viewModel.HasSearchError);
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
        Assert.True(viewModel.ShowAddForm);
    }

    /// <summary>
    /// Verifies that the direct-add command does not run in edit mode — adding
    /// a feed from the edit form's URL would silently discard the in-progress
    /// edit, the same reason the search is blocked there.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_InEditMode_DoesNotAddFeed()
    {
        var feedId = await SeedFeedAsync("Edit Me", "https://example.com/edit-me");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Feeds.First(f => f.Id == feedId));
        viewModel.NewUrl = "https://example.com/changed.xml";

        Assert.False(viewModel.DirectAddCommand.CanExecute(null));

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        Assert.Null(await _feedRepository.GetByUrlAsync("https://example.com/changed.xml"));
        Assert.Single(viewModel.Feeds);
        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.IsEditMode);
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
    /// Verifies that a successful save in edit mode resets the notifications flag
    /// to its default.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_ResetsFeedNotificationsEnabled()
    {
        var feedId = await SeedFeedAsync(notificationsEnabled: false);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Feeds.First(f => f.Id == feedId));
        Assert.False(viewModel.FeedNotificationsEnabled);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that the save command never adds feeds: the sheet's save button
    /// only exists in edit mode and adding runs exclusively through the search,
    /// subscribe and direct-add flows.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_WithoutSelectedFeed_DoesNotAddFeed()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/new-feed";
        viewModel.NewTitle = "New Feed";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null(await _feedRepository.GetByUrlAsync("https://example.com/new-feed"));
        Assert.Empty(viewModel.Feeds);
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
        Assert.False(viewModel.ShowAddForm);
        Assert.False(viewModel.IsEditMode);
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
    /// Verifies that the search does not run in edit mode — triggering it (e.g.
    /// via the entry's ReturnCommand) must not silently discard an in-progress
    /// edit by swapping the sheet for the results view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_InEditMode_DoesNotDiscardEdit()
    {
        var feedId = await SeedFeedAsync("Edit Me", "https://example.com/edit-me");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Feeds.First(f => f.Id == feedId));

        Assert.False(viewModel.SearchCommand.CanExecute(null));

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(0, _searchService.CallCount);
        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.IsEditMode);
        Assert.Equal("https://example.com/edit-me", viewModel.NewUrl);
        Assert.False(viewModel.ShowSearchResults);
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
        Assert.Null(await _feedRepository.GetByUrlAsync("https://example.com/feed"));
    }

    /// <summary>
    /// Verifies that an empty result for a valid URL input offers the direct-add
    /// confirmation and persists the feed immediately with a file-name title and
    /// the fixed defaults when confirmed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_NoResultsAndValidUrl_Confirmed_AddsFeedWithFileNameTitle()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        string? invokedUrl = null;
        viewModel.ConfirmDirectAddAsync = url =>
        {
            invokedUrl = url;
            return Task.FromResult(true);
        };
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://heise.de/rss/heise-atom.xml";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("https://heise.de/rss/heise-atom.xml", invokedUrl);
        var saved = await _feedRepository.GetByUrlAsync("https://heise.de/rss/heise-atom.xml");
        Assert.NotNull(saved);
        Assert.Equal("heise-atom.xml", saved.Title);
        Assert.Null(saved.CategoryId);
        Assert.True(saved.NotificationsEnabled);
        Assert.False(viewModel.ShowAddForm);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that a confirmed direct add of an already stored URL surfaces the
    /// duplicate error inside the still-open sheet without persisting a feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError()
    {
        await SeedFeedAsync("Existing", "https://example.com/feed");
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ => Task.FromResult(true);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com/feed";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
        Assert.True(viewModel.ShowAddForm);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that a confirmed direct add of an already stored URL reached via
    /// the empty-results path closes the search results view, so the reopened
    /// sheet sits above the feed list instead of the empty results view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_NoResultsAndConfirmedDuplicate_ClosesResultsView()
    {
        await SeedFeedAsync("Existing", "https://example.com/feed");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ConfirmDirectAddAsync = _ => Task.FromResult(true);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com/feed";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        Assert.False(viewModel.ShowSearchResults);
        Assert.Empty(viewModel.SearchResults);
        Assert.Single(viewModel.Feeds);
    }

    /// <summary>
    /// Verifies that showing search results closes the add-feed sheet so the
    /// overlay does not cover the results view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenResultsShown_ClosesAddForm()
    {
        _searchService.NextResults =
        [
            new FeedSearchResult { FeedUrl = "https://example.com/rss", Title = "Example Feed", MatchKind = FeedSearchMatchKind.Directory },
        ];
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowSearchResults);
        Assert.False(viewModel.ShowAddForm);
    }

    /// <summary>
    /// Verifies that an unavailable search for a domain input keeps the sheet
    /// open and reports the retry hint inside it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError()
    {
        _searchService.NextException = new FeedSearchUnavailableException("down");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "example.com";

        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.HasSearchError);
        Assert.Equal(AppResources.FeedSearchUnavailableRetry, viewModel.SearchErrorMessage);
    }

    /// <summary>
    /// Verifies that declining the direct-add confirmation keeps the form state
    /// untouched, hides the search results view and does not persist a feed.
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
        Assert.Null(await _feedRepository.GetByUrlAsync("https://other.example.com/feed"));
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
    /// Verifies that a search failure is discarded when the user changed the URL
    /// while the search was still in flight — the stale error of the abandoned
    /// query must not surface. This covers the stale-input check in the catch
    /// path; the check itself is a duplicated guard, so the test pins the
    /// observable contract rather than a defect that can fail beforehand.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchCommand_WhenUrlChangesDuringFailedSearch_DiscardsStaleError()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<FeedSearchResult>>();
        _searchService.PendingResult = pending;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com";

        var searchTask = viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://other.example.com";
        pending.SetException(new FeedSearchUnavailableException("down"));
        await searchTask;

        Assert.False(viewModel.HasSearchError);
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
        Assert.False(viewModel.ShowSearchResults);
        Assert.False(viewModel.IsSearching);
    }

    /// <summary>
    /// Verifies that subscribing a result persists a feed built from the result
    /// with the fixed defaults (no category, notifications enabled), ignoring the
    /// form state, then resets the search view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_PersistsFeedFromResult()
    {
        var categoryId = Guid.NewGuid();
        await _categoryRepository.AddAsync(new Category { Id = categoryId, Name = "Tech" });
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
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
        Assert.Null(saved.CategoryId);
        Assert.True(saved.NotificationsEnabled);
        Assert.Equal(FeedHealth.Ok, saved.HealthStatus);
        Assert.Empty(viewModel.SearchResults);
        Assert.False(viewModel.ShowSearchResults);
        Assert.Single(viewModel.Feeds);
        Assert.True(viewModel.FeedNotificationsEnabled);
    }

    /// <summary>
    /// Verifies that a result without a title stores the file name of the feed
    /// URL as placeholder title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var result = new FeedSearchResult
        {
            FeedUrl = "https://heise.de/rss/heise-atom.xml",
            MatchKind = FeedSearchMatchKind.Discovered,
        };

        await viewModel.SubscribeResultCommand.ExecuteAsync(result);

        var saved = await _feedRepository.GetByUrlAsync("https://heise.de/rss/heise-atom.xml");
        Assert.NotNull(saved);
        Assert.Equal("heise-atom.xml", saved.Title);
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

    /// <summary>
    /// Verifies that the open command shows the sheet in add mode and clears
    /// leftover error messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task OpenAddFormCommand_ShowsForm()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.ErrorMessage = AppResources.ErrorFeedDuplicate;
        viewModel.SearchErrorMessage = AppResources.FeedSearchUnavailableRetry;

        viewModel.OpenAddFormCommand.Execute(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.False(viewModel.IsEditMode);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasSearchError);
    }

    /// <summary>
    /// Verifies that opening the add sheet after an abandoned edit clears the
    /// stale edit state, keeping the invariant "add mode implies no selected feed".
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task OpenAddFormCommand_AfterEdit_ClearsStaleEditState()
    {
        var feedId = await SeedFeedAsync("Edit Me", "https://example.com/edit-me");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Feeds.First(f => f.Id == feedId));
        // Simulate the search having closed the sheet without resetting the form.
        viewModel.ShowAddForm = false;

        viewModel.OpenAddFormCommand.Execute(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.False(viewModel.IsEditMode);
        Assert.Null(viewModel.SelectedFeed);
        Assert.Equal(string.Empty, viewModel.NewUrl);
        Assert.Equal(string.Empty, viewModel.NewTitle);
    }

    /// <summary>
    /// Verifies that the close command hides the sheet and resets the form state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CloseAddFormCommand_ResetsFormAndHidesForm()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.EditCommand.ExecuteAsync(feed);
        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.IsEditMode);

        viewModel.CloseAddFormCommand.Execute(null);

        Assert.False(viewModel.ShowAddForm);
        Assert.False(viewModel.IsEditMode);
        Assert.Equal(string.Empty, viewModel.NewUrl);
        Assert.Null(viewModel.SelectedFeed);
    }

    /// <summary>
    /// Verifies that closing the sheet clears both error channels — the same
    /// labels are bound in the page header above the feed list, so an error
    /// shown inside the sheet must not stay visible after the sheet closes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CloseAddFormCommand_WhenSheetShowsErrors_ClearsErrorChannels()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.OpenAddFormCommand.Execute(null);

        // A rejected direct add raises the validation error inside the open
        // sheet; the search error simulates a still-shown in-sheet failure.
        await viewModel.DirectAddCommand.ExecuteAsync(null);
        viewModel.SearchErrorMessage = AppResources.FeedSearchUnavailableRetry;
        Assert.True(viewModel.HasError);
        Assert.True(viewModel.HasSearchError);

        viewModel.CloseAddFormCommand.Execute(null);

        Assert.False(viewModel.ShowAddForm);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
        Assert.False(viewModel.HasSearchError);
        Assert.Equal(string.Empty, viewModel.SearchErrorMessage);
    }

    /// <summary>
    /// Verifies that editing a feed opens the sheet in edit mode with the feed
    /// values loaded into the form.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EditAsync_OpensSheetInEditMode()
    {
        var feedId = await SeedFeedAsync("Edit Me", "https://example.com/edit-me");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        await viewModel.EditCommand.ExecuteAsync(feed);

        Assert.True(viewModel.IsEditMode);
        Assert.True(viewModel.ShowAddForm);
        Assert.Equal("https://example.com/edit-me", viewModel.NewUrl);
        Assert.Equal("Edit Me", viewModel.NewTitle);
    }

    /// <summary>
    /// Verifies that a failed save in edit mode keeps the sheet open and shows
    /// the validation error inside it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError()
    {
        var feedId = await SeedFeedAsync();
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.EditCommand.ExecuteAsync(feed);
        viewModel.NewUrl = "not a url";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedUrlInvalid, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/rss", saved?.Url);
    }

    /// <summary>
    /// Verifies that a duplicate URL in edit mode keeps the sheet open, shows the
    /// duplicate error inside it and does not update the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_EditMode_WhenDuplicate_KeepsSheetOpenAndSetsError()
    {
        var feedId = await SeedFeedAsync("Feed A", "https://example.com/feed-a");
        await SeedFeedAsync("Feed B", "https://example.com/feed-b");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.EditCommand.ExecuteAsync(feed);
        viewModel.NewUrl = "https://example.com/feed-b";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorFeedDuplicate, viewModel.ErrorMessage);
        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("https://example.com/feed-a", saved?.Url);
    }

    /// <summary>
    /// Verifies that saving an edited feed keeps its stored category assignment
    /// — the sheet no longer offers a category picker, so the stored value is
    /// the only source of truth. Note: the original drift scenario (stale
    /// <c>SelectedCategory</c> preserve-state reset by <c>LoadAsync</c>) cannot
    /// be reproduced as a failing test because that write-only state was removed
    /// with the picker; this test pins the surviving contract.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveCommand_EditMode_PreservesCategoryId()
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.EditCommand.ExecuteAsync(feed);

        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(categoryId, saved?.CategoryId);
    }

    /// <summary>
    /// Verifies that renaming a feed persists the new title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_UpdatesTitle()
    {
        var feedId = await SeedFeedAsync("Old Title", "https://example.com/rss");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        await viewModel.RenameFeedAsync(feed, "  New Title  ");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal("New Title", saved?.Title);
        Assert.False(viewModel.HasError);
        Assert.Equal("New Title", viewModel.Feeds.Single().Title);
    }

    /// <summary>
    /// Verifies that an empty rename title reports the validation error and keeps
    /// the current title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RenameFeedAsync_EmptyTitle_SetsErrorAndKeepsTitle()
    {
        var feedId = await SeedFeedAsync("Keep Me", "https://example.com/rss");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

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
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.RenameFeedAsync(null, "New Title");

        Assert.False(viewModel.HasError);
        Assert.Empty(viewModel.Feeds);
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        var category = viewModel.Categories.First(c => c.Id == categoryId);

        await viewModel.ChangeFeedCategoryAsync(feed, category);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.Equal(categoryId, saved?.CategoryId);
        Assert.Equal("Tech", viewModel.Feeds.Single().CategoryName);
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);
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
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.ChangeFeedCategoryAsync(null, null);

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Null(saved.CategoryId);
    }

    /// <summary>
    /// Verifies that repeated names get a counter suffix so each option label
    /// is unique.
    /// </summary>
    [Fact]
    public void MakeUniqueOptionLabels_DuplicateNames_AppendsCounterSuffix()
    {
        var labels = FeedsViewModel.MakeUniqueOptionLabels(["News", "News"]);

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
        var labels = FeedsViewModel.MakeUniqueOptionLabels(["News", "News", "News (2)"]);

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
        var labels = FeedsViewModel.MakeUniqueOptionLabels(["News (2)", "News", "News"]);

        Assert.Equal(3, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("News (2)", labels[0]);
        Assert.Equal("News", labels[1]);
    }

    /// <summary>
    /// Verifies that a direct add stores the favicon discovered from the feed
    /// URL's authority.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_StoresFaviconUrl()
    {
        _feedIconService.NextResult = "https://example.com/favicon.ico";
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/rss";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/rss");
        Assert.NotNull(saved);
        Assert.Equal("https://example.com/favicon.ico", saved.FaviconUrl);
        Assert.Equal("https://example.com", Assert.Single(_feedIconService.RequestedSiteUrls));
    }

    /// <summary>
    /// Verifies that subscribing a search result uses the result's site URL for
    /// the favicon lookup.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SubscribeResultCommand_UsesSiteUrlForFaviconLookup()
    {
        _feedIconService.NextResult = "https://site.example/icon.png";
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        var result = new FeedSearchResult
        {
            FeedUrl = "https://feeds.example.com/rss",
            Title = "Example Feed",
            SiteUrl = "https://site.example",
            MatchKind = FeedSearchMatchKind.Directory,
        };

        await viewModel.SubscribeResultCommand.ExecuteAsync(result);

        var saved = await _feedRepository.GetByUrlAsync("https://feeds.example.com/rss");
        Assert.NotNull(saved);
        Assert.Equal("https://site.example/icon.png", saved.FaviconUrl);
        Assert.Equal("https://site.example", Assert.Single(_feedIconService.RequestedSiteUrls));
    }

    /// <summary>
    /// Verifies that a failing favicon lookup does not prevent the feed from
    /// being added.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_IconLookupFails_FeedStillAdded()
    {
        _feedIconService.NextException = new InvalidOperationException("lookup failed");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewUrl = "https://example.com/rss";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/rss");
        Assert.NotNull(saved);
        Assert.Null(saved.FaviconUrl);
        Assert.False(viewModel.HasError);
    }

    /// <summary>
    /// Verifies that the favicon lookup is skipped while offline.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DirectAddCommand_Offline_SkipsIconLookup()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        viewModel.OpenAddFormCommand.Execute(null);
        viewModel.NewUrl = "https://example.com/rss";

        await viewModel.DirectAddCommand.ExecuteAsync(null);

        var saved = await _feedRepository.GetByUrlAsync("https://example.com/rss");
        Assert.NotNull(saved);
        Assert.Null(saved.FaviconUrl);
        Assert.Empty(_feedIconService.RequestedSiteUrls);
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        await viewModel.RenameFeedAsync(feed, "New Title");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Equal("New Title", saved.Title);
        Assert.Equal("https://example.com/favicon.ico", saved.FaviconUrl);
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        var message = viewModel.GetFeedErrorMessage(feed);

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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        var message = viewModel.GetFeedErrorMessage(feed);

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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        var message = viewModel.GetFeedErrorMessage(feed);

        Assert.StartsWith(AppResources.FeedErrorKindParse, message);
        Assert.Contains("Synchronization failed: invalid xml", message);
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
        await viewModel.LoadCommand.ExecuteAsync(null);
        var feed = viewModel.Feeds.First(f => f.Id == feedId);

        await viewModel.RenameFeedAsync(feed, "New Title");

        var saved = await _feedRepository.GetByIdAsync(feedId);
        Assert.NotNull(saved);
        Assert.Equal("New Title", saved.Title);
        Assert.Equal(FeedSyncErrorKind.HttpStatus, saved.LastErrorKind);
        Assert.Equal("Synchronization failed: 404", saved.LastErrorMessage);
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
