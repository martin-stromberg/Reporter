using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="UnreadViewModel"/> class.
/// </summary>
public class UnreadViewModelTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly FakeFeedSyncService _feedSyncService;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly UnreadViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests"/> class.
    /// </summary>
    public UnreadViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory);
        _categoryRepository = new CategoryRepository(_factory);
        _feedSyncService = new FakeFeedSyncService();
        _networkStatusService = new FakeNetworkStatusService();
        _viewModel = new UnreadViewModel(_itemRepository, _categoryRepository, _feedSyncService, _networkStatusService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<(Guid FeedId, Guid? CategoryId)> SeedFeedAndCategoryAsync()
    {
        var categoryId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Categories.Add(new Entities.Category { Id = categoryId, Name = "News" });
            await context.SaveChangesAsync();
        }

        var feedId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Feeds.Add(new Entities.Feed
            {
                Id = feedId,
                Url = "https://example.com/feed",
                Title = "Example Feed",
                CategoryId = categoryId,
            });
            await context.SaveChangesAsync();
        }

        return (feedId, categoryId);
    }

    /// <summary>
    /// Verifies that LoadCommand loads unread articles and category filters.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_PopulatesArticlesAndCategories()
    {
        var (feedId, _) = await SeedFeedAndCategoryAsync();
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Sample",
            GuidOrHash = "sample",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.Articles);
        Assert.NotEmpty(_viewModel.Categories);
    }

    /// <summary>
    /// Verifies that selecting a category filter reduces the article list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SelectCategoryCommand_FiltersArticles()
    {
        var (feedId, categoryId) = await SeedFeedAndCategoryAsync();
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Sample",
            GuidOrHash = "sample",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);
        var category = _viewModel.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        Assert.NotNull(category);

        await _viewModel.SelectCategoryCommand.ExecuteAsync(category);

        Assert.Single(_viewModel.Articles);
    }

    /// <summary>
    /// Verifies that MarkReadCommand removes the article from the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_RemovesArticle()
    {
        var (feedId, _) = await SeedFeedAndCategoryAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToRead",
            GuidOrHash = "toread",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        };
        await _itemRepository.AddAsync(item);

        await _viewModel.LoadCommand.ExecuteAsync(null);
        var article = _viewModel.Articles.FirstOrDefault(a => a.Id == item.Id);
        Assert.NotNull(article);

        await _viewModel.MarkReadCommand.ExecuteAsync(article);

        Assert.DoesNotContain(_viewModel.Articles, a => a.Id == item.Id);
    }

    /// <summary>
    /// Verifies that MarkAllReadCommand clears the article list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAllReadCommand_ClearsArticles()
    {
        var (feedId, _) = await SeedFeedAndCategoryAsync();
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Sample",
            GuidOrHash = "sample",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.NotEmpty(_viewModel.Articles);

        await _viewModel.MarkAllReadCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Articles);
    }

    /// <summary>
    /// Verifies that RefreshCommand invokes the sync service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_InvokesSyncService()
    {
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(_feedSyncService.SyncAllCalled);
    }

    /// <summary>
    /// Verifies that RefreshCommand skips the sync while offline without raising a
    /// separate error message — the persistent offline hint already communicates the state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenOffline_SkipsSyncWithoutError()
    {
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsOnline);
        Assert.False(_feedSyncService.SyncAllCalled);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasError);
    }

    /// <summary>
    /// Verifies that a thrown sync exception surfaces a localized generic message
    /// instead of the raw exception text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenSyncThrows_SetsLocalizedSyncError()
    {
        _feedSyncService.NextException = new InvalidOperationException("raw provider message");

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.SyncStatusError, _viewModel.ErrorMessage);
    }

    /// <summary>
    /// Verifies that a connectivity status change clears a previously shown sync error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ConnectivityChanged_ClearsErrorMessage()
    {
        _feedSyncService.NextResult = new SyncResult(FeedHealth.Error, 0, "Network error");
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.True(_viewModel.HasError);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasError);
    }

    /// <summary>
    /// Verifies that the IsOnline property follows connectivity change events.
    /// </summary>
    [Fact]
    public void ConnectivityChanged_UpdatesIsOnline()
    {
        Assert.True(_viewModel.IsOnline);

        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.False(_viewModel.IsOnline);

        _networkStatusService.IsOnline = true;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.True(_viewModel.IsOnline);
    }

    /// <summary>
    /// Verifies that the regular sync path runs again once the network is back.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RefreshCommand_WhenBackOnline_InvokesSyncService()
    {
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.False(_feedSyncService.SyncAllCalled);

        _networkStatusService.IsOnline = true;
        _networkStatusService.RaiseConnectivityChanged();
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.True(_feedSyncService.SyncAllCalled);
    }

    /// <summary>
    /// Verifies that ToggleSavedCommand toggles the saved flag on the list item in place.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_TogglesFlagInPlace()
    {
        var (feedId, _) = await SeedFeedAndCategoryAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToToggle",
            GuidOrHash = "totoggle",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        };
        await _itemRepository.AddAsync(item);

        await _viewModel.LoadCommand.ExecuteAsync(null);
        var article = _viewModel.Articles.FirstOrDefault(a => a.Id == item.Id);
        Assert.NotNull(article);
        Assert.False(article.IsSavedForLater);

        await _viewModel.ToggleSavedCommand.ExecuteAsync(article);

        var toggled = _viewModel.Articles.FirstOrDefault(a => a.Id == item.Id);
        Assert.NotNull(toggled);
        Assert.True(toggled.IsSavedForLater);
        var persisted = await _itemRepository.GetByIdAsync(item.Id);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsSavedForLater);
    }

    private sealed class FakeFeedSyncService : IFeedSyncService
    {
        public bool SyncAllCalled { get; private set; }

        /// <summary>
        /// Gets or sets the result returned by the fake service.
        /// </summary>
        /// <value>The sync result returned to the caller.</value>
        public SyncResult NextResult { get; set; } = new SyncResult(FeedHealth.Ok, 0);

        /// <summary>
        /// Gets or sets the exception thrown by the fake service, if any.
        /// </summary>
        public Exception? NextException { get; set; }

        public Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)
        {
            return NextException is not null
                ? Task.FromException<SyncResult>(NextException)
                : Task.FromResult(NextResult);
        }

        public Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
        {
            SyncAllCalled = true;
            return NextException is not null
                ? Task.FromException<SyncResult>(NextException)
                : Task.FromResult(NextResult);
        }
    }
}
