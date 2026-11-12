// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the filter chip counters of the <see cref="UnreadViewModel"/> class
/// after marking an article as read.
/// </summary>
public class UnreadViewModelTests_ChipCounts : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly UnreadViewModel _viewModel;
    private Guid _categoryId;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests_ChipCounts"/> class.
    /// </summary>
    public UnreadViewModelTests_ChipCounts()
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
    /// Verifies that marking an article as read under "All" also decrements its category chip.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_UnderAllFilter_DecrementsCategoryChip()
    {
        await SeedAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);

        Assert.Equal(1, _viewModel.Categories.Single(c => c.CategoryId == _categoryId).Count);
    }

    /// <summary>
    /// Verifies that marking an article as read under a category filter also decrements the "All" chip.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_UnderCategoryFilter_DecrementsAllChip()
    {
        await SeedAsync(2);
        await _viewModel.LoadCommand.ExecuteAsync(null);
        await _viewModel.SelectCategoryCommand.ExecuteAsync(_viewModel.Categories.Single(c => c.CategoryId == _categoryId));

        await _viewModel.MarkReadCommand.ExecuteAsync(_viewModel.Articles[0]);

        Assert.Equal(1, _viewModel.Categories.Single(c => c.CategoryId is null).Count);
        Assert.Equal(1, _viewModel.Categories.Single(c => c.CategoryId == _categoryId).Count);
    }

    private async Task SeedAsync(int count)
    {
        _categoryId = Guid.NewGuid();
        var feedId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Categories.Add(new Entities.Category { Id = _categoryId, Name = "News" });
            context.Feeds.Add(new Entities.Feed { Id = feedId, Url = "https://example.com/feed", Title = "Example Feed", CategoryId = _categoryId });
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
