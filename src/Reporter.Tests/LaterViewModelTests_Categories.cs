// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the category filter tabs, counters and list state of the <see cref="LaterViewModel"/> class.
/// </summary>
public class LaterViewModelTests_Categories : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly LaterViewModel _viewModel;
    private readonly Guid _newsId = Guid.NewGuid();
    private readonly Guid _techId = Guid.NewGuid();
    private readonly Guid _emptyId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModelTests_Categories"/> class.
    /// </summary>
    public LaterViewModelTests_Categories()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory, new FakeItemContentStore());
        _viewModel = new LaterViewModel(_itemRepository, new CategoryRepository(_factory), new FakeNetworkStatusService());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that "All" comes first and only categories with saved articles get a tab.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_CreatesTabsOnlyForCategoriesWithSavedItems()
    {
        await SeedAsync();

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Null(_viewModel.Categories[0].CategoryId);
        Assert.Equal(3, _viewModel.Categories.Count);
        Assert.DoesNotContain(_viewModel.Categories, c => c.CategoryId == _emptyId);
    }

    /// <summary>
    /// Verifies that the tab counters reflect the saved articles per category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_SetsSavedCounts()
    {
        await SeedAsync();

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, _viewModel.SavedCount);
        Assert.Equal(2, _viewModel.Categories.Single(c => c.CategoryId == _newsId).Count);
        Assert.True(_viewModel.Categories.Single(c => c.CategoryId is null).IsSelected);
    }

    /// <summary>
    /// Verifies that selecting a tab filters the saved articles by category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SelectCategoryCommand_FiltersSavedItems()
    {
        await SeedAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.SelectCategoryCommand.ExecuteAsync(_viewModel.Categories.Single(c => c.CategoryId == _techId));

        Assert.Equal("Tech saved", Assert.Single(_viewModel.SavedItems).Title);
    }

    /// <summary>
    /// Verifies that removing a saved article decrements the total and the affected tabs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_DecrementsCounters()
    {
        await SeedAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        var newsItem = _viewModel.SavedItems.First(i => i.CategoryId == _newsId);

        await _viewModel.ToggleSavedCommand.ExecuteAsync(newsItem);

        Assert.Equal(2, _viewModel.SavedCount);
        Assert.Equal(2, _viewModel.Categories.Single(c => c.CategoryId is null).Count);
        Assert.Equal(1, _viewModel.Categories.Single(c => c.CategoryId == _newsId).Count);
    }

    /// <summary>
    /// Verifies that the empty state is shown without saved articles.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithoutSavedItems_ShowsEmptyState()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.ShowEmptyState);
        Assert.False(_viewModel.ShowSkeleton);
    }

    /// <summary>
    /// Verifies that the filter tabs are hidden when no category has saved articles.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithoutCategorizedSavedItems_HidesTabs()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(_viewModel.ShowCategoryTabs);
    }

    /// <summary>
    /// Verifies that the repository counts saved articles per category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSavedCountAsync_WithCategory_CountsOnlyThatCategory()
    {
        await SeedAsync();

        Assert.Equal(1, await _itemRepository.GetSavedCountAsync(_techId));
    }

    private async Task SeedAsync()
    {
        var newsFeed = Guid.NewGuid();
        var techFeed = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Categories.AddRange(
                new Entities.Category { Id = _newsId, Name = "News" },
                new Entities.Category { Id = _techId, Name = "Tech" },
                new Entities.Category { Id = _emptyId, Name = "Empty" });
            context.Feeds.AddRange(
                new Entities.Feed { Id = newsFeed, Url = "https://example.com/news", Title = "News Feed", CategoryId = _newsId },
                new Entities.Feed { Id = techFeed, Url = "https://example.com/tech", Title = "Tech Feed", CategoryId = _techId });
            await context.SaveChangesAsync();
        }

        await AddAsync(newsFeed, "News saved 1", saved: true, 1);
        await AddAsync(newsFeed, "News saved 2", saved: true, 2);
        await AddAsync(newsFeed, "News unsaved", saved: false, 3);
        await AddAsync(techFeed, "Tech saved", saved: true, 4);
    }

    private Task AddAsync(Guid feedId, string title, bool saved, int hour)
    {
        return _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = title,
            GuidOrHash = title,
            IsRead = false,
            IsSavedForLater = saved,
            PublishedAt = new DateTime(2026, 1, 1).AddHours(hour),
        });
    }
}
