// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the list state (empty state, card position, current card) of the
/// <see cref="UnreadViewModel"/> class.
/// </summary>
public class UnreadViewModelTests_ListState : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly UnreadViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests_ListState"/> class.
    /// </summary>
    public UnreadViewModelTests_ListState()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory, new FakeItemContentStore());
        _networkStatusService = new FakeNetworkStatusService();
        _viewModel = new UnreadViewModel(
            _itemRepository,
            new CategoryRepository(_factory),
            new FakeFeedSyncService(),
            new SettingsRepository(_factory),
            _networkStatusService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that the empty state is shown when no unread article exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithoutArticles_ShowsEmptyState()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.ShowEmptyState);
        Assert.False(_viewModel.ShowSkeleton);
    }

    /// <summary>
    /// Verifies that the empty state is hidden as soon as articles are loaded.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithArticles_HidesEmptyState()
    {
        await SeedArticlesAsync(1);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasArticles);
        Assert.False(_viewModel.ShowEmptyState);
    }

    /// <summary>
    /// Verifies that the card position indicator reflects the current position and total.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CardPositionText_ReflectsPositionAndTotal()
    {
        await SeedArticlesAsync(3);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.CardPosition = 1;

        Assert.Equal(string.Format(AppResources.UnreadCardPositionFormat, 2, 3), _viewModel.CardPositionText);
    }

    /// <summary>
    /// Verifies that toggling the saved state of the current card refreshes the current card.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_OnCurrentCard_UpdatesCurrentCard()
    {
        await SeedArticlesAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.CardPosition = 1;

        await _viewModel.ToggleSavedCommand.ExecuteAsync(_viewModel.CurrentCard);

        Assert.True(_viewModel.CurrentCard!.IsSavedForLater);
    }

    /// <summary>
    /// Verifies that marking a stale copy of an article as read still removes the listed article.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_WithStaleCopy_RemovesListedArticle()
    {
        await SeedArticlesAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        var staleCopy = _viewModel.Articles[0].CopyWith();

        await _viewModel.MarkReadCommand.ExecuteAsync(staleCopy);

        Assert.DoesNotContain(_viewModel.Articles, a => a.Id == staleCopy.Id);
    }

    /// <summary>
    /// Verifies that the status line reports the offline state.
    /// </summary>
    [Fact]
    public void SyncStatusText_WhenOffline_ReportsOffline()
    {
        _networkStatusService.IsOnline = false;
        _networkStatusService.RaiseConnectivityChanged();

        Assert.Equal(AppResources.UnreadOffline, _viewModel.SyncStatusText);
    }

    private async Task SeedArticlesAsync(int count)
    {
        var feedId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Feeds.Add(new Entities.Feed { Id = feedId, Url = "https://example.com/feed", Title = "Example Feed" });
            await context.SaveChangesAsync();
        }

        for (var i = 0; i < count; i++)
        {
            await _itemRepository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Article {i}",
                GuidOrHash = $"article-{i}",
                IsRead = false,
                IsSavedForLater = false,
                PublishedAt = new DateTime(2026, 1, 1).AddHours(i),
            });
        }
    }
}
