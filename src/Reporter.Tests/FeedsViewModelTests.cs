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
        return new FeedsViewModel(_feedRepository, _categoryRepository, _syncService, _networkStatusService);
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
    /// sync error channel, not the form validation channel.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenSyncReturnsError_SetsSyncErrorMessage()
    {
        var feedId = await SeedFeedAsync();
        _syncService.NextResult = new SyncResult(FeedHealth.Error, 0, "Network error");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        var feed = viewModel.Feeds.First(f => f.Id == feedId);
        await viewModel.RefreshCommand.ExecuteAsync(feed);

        Assert.True(viewModel.HasSyncError);
        Assert.Equal("Network error", viewModel.SyncErrorMessage);
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
