// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the card stack navigation of the <see cref="UnreadViewModel"/> class.
/// </summary>
public class UnreadViewModelTests_CardStack : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly UnreadViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests_CardStack"/> class.
    /// </summary>
    public UnreadViewModelTests_CardStack()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory, new FakeItemContentStore());
        _viewModel = new UnreadViewModel(
            _itemRepository,
            new CategoryRepository(_factory),
            new FakeFeedSyncService(),
            new SettingsRepository(_factory),
            new FakeNetworkStatusService());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that the stack starts with the first article on top and the second underneath.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_ShowsFirstArticleOnTop()
    {
        await SeedArticlesAsync(3);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Same(_viewModel.Articles[0], _viewModel.CurrentCard);
        Assert.Same(_viewModel.Articles[1], _viewModel.NextCard);
    }

    /// <summary>
    /// Verifies that NextCardCommand advances the stack by one article.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NextCardCommand_AdvancesStack()
    {
        await SeedArticlesAsync(3);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.NextCardCommand.Execute(null);

        Assert.Equal(1, _viewModel.CardPosition);
        Assert.Same(_viewModel.Articles[1], _viewModel.CurrentCard);
    }

    /// <summary>
    /// Verifies that the stack cannot advance past the last article.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task NextCardCommand_OnLastCard_CannotExecute()
    {
        await SeedArticlesAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NextCardCommand.Execute(null);

        Assert.False(_viewModel.NextCardCommand.CanExecute(null));
        Assert.Null(_viewModel.NextCard);
    }

    /// <summary>
    /// Verifies that PreviousCardCommand moves the stack back by one article.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task PreviousCardCommand_MovesBack()
    {
        await SeedArticlesAsync(3);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.CardPosition = 2;

        _viewModel.PreviousCardCommand.Execute(null);

        Assert.Equal(1, _viewModel.CardPosition);
    }

    /// <summary>
    /// Verifies that out-of-range positions are clamped to the list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CardPosition_OutOfRange_IsClamped()
    {
        await SeedArticlesAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.CardPosition = 10;

        Assert.Equal(1, _viewModel.CardPosition);
    }

    /// <summary>
    /// Verifies that marking the top card as read brings the next article to the top.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_OnTopCard_PromotesNextArticle()
    {
        await SeedArticlesAsync(3);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        var expectedNext = _viewModel.NextCard;

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.CurrentCard);

        Assert.Equal(expectedNext!.Id, _viewModel.CurrentCard!.Id);
    }

    /// <summary>
    /// Verifies that reloading the list keeps the same article on top of the stack.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_Reload_KeepsTopArticle()
    {
        await SeedArticlesAsync(3);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.CardPosition = 2;
        var topId = _viewModel.CurrentCard!.Id;

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(topId, _viewModel.CurrentCard!.Id);
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
