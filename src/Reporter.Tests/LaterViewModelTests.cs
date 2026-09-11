using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="LaterViewModel"/> class.
/// </summary>
public class LaterViewModelTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _itemRepository;
    private readonly LaterViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModelTests"/> class.
    /// </summary>
    public LaterViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory);
        _viewModel = new LaterViewModel(_itemRepository);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that LoadCommand loads only items saved for later.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_PopulatesOnlySavedItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        });
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Not Saved",
            GuidOrHash = "notsaved",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 2),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.SavedItems);
        Assert.Equal("Saved", _viewModel.SavedItems[0].Title);
    }

    /// <summary>
    /// Verifies that LoadCommand orders saved items by PublishedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_OrdersByPublishedAtDescending()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Older",
            GuidOrHash = "older",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        });
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Newer",
            GuidOrHash = "newer",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 2),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.SavedItems.Count);
        Assert.Equal("Newer", _viewModel.SavedItems[0].Title);
        Assert.Equal("Older", _viewModel.SavedItems[1].Title);
    }

    /// <summary>
    /// Verifies that ToggleSavedCommand removes the item from the saved list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_RemovesItemFromSavedItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        };
        await _itemRepository.AddAsync(item);

        await _viewModel.LoadCommand.ExecuteAsync(null);
        var savedItem = _viewModel.SavedItems.FirstOrDefault(i => i.Id == item.Id);
        Assert.NotNull(savedItem);

        await _viewModel.ToggleSavedCommand.ExecuteAsync(savedItem);

        Assert.Empty(_viewModel.SavedItems);
    }

    /// <summary>
    /// Verifies that ToggleSavedCommand with a null item does nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedCommand_NullItem_DoesNothing()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _itemRepository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);
        await _viewModel.ToggleSavedCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.SavedItems);
    }

    /// <summary>
    /// Verifies that MarkReadCommand marks the item as read and keeps it in the saved list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_SetsReadAndKeepsItemInList()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        };
        await _itemRepository.AddAsync(item);

        await _viewModel.LoadCommand.ExecuteAsync(null);
        var savedItem = _viewModel.SavedItems.FirstOrDefault(i => i.Id == item.Id);
        Assert.NotNull(savedItem);

        await _viewModel.MarkReadCommand.ExecuteAsync(savedItem);

        Assert.Single(_viewModel.SavedItems);
        var persisted = await _itemRepository.GetByIdAsync(item.Id);
        Assert.NotNull(persisted);
        Assert.True(persisted.IsRead);
    }
}
