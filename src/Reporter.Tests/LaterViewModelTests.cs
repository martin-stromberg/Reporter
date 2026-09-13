// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
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
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly LaterViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModelTests"/> class.
    /// </summary>
    public LaterViewModelTests()
    {
        _factory = new TestDbContextFactory();
        _itemRepository = new ItemRepository(_factory);
        _networkStatusService = new FakeNetworkStatusService();
        _viewModel = new LaterViewModel(_itemRepository, _networkStatusService);
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
    /// Verifies that LoadMoreCommand appends the next page of saved items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_AppendsNextPage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        for (var i = 0; i < LaterViewModel.PageSize + 5; i++)
        {
            await _itemRepository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Saved {i:D2}",
                GuidOrHash = $"saved-{i}",
                IsRead = false,
                IsSavedForLater = true,
                PublishedAt = new DateTime(2026, 1, 1).AddDays(i),
            });
        }

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(LaterViewModel.PageSize, _viewModel.SavedItems.Count);
        Assert.True(_viewModel.HasMore);

        await _viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(LaterViewModel.PageSize + 5, _viewModel.SavedItems.Count);
        Assert.False(_viewModel.HasMore);
    }

    /// <summary>
    /// Verifies that LoadMoreCommand does nothing when no further page exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadMoreCommand_WhenNoMoreItems_DoesNothing()
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
        Assert.False(_viewModel.HasMore);
        Assert.False(_viewModel.LoadMoreCommand.CanExecute(null));

        await _viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.SavedItems);
    }

    /// <summary>
    /// Verifies that MarkReadCommand replaces the item in place with a read copy.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkReadCommand_UpdatesItemInPlace()
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
        Assert.False(savedItem.IsRead);

        await _viewModel.MarkReadCommand.ExecuteAsync(savedItem);

        Assert.Single(_viewModel.SavedItems);
        Assert.True(_viewModel.SavedItems[0].IsRead);
        Assert.Equal(item.Id, _viewModel.SavedItems[0].Id);
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

    /// <summary>
    /// Verifies that a failing repository surfaces a localized load error via
    /// ErrorMessage/HasError instead of leaving the page silently empty.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WhenRepositoryFails_SetsLocalizedErrorMessage()
    {
        var viewModel = new LaterViewModel(new FailingItemRepository(_itemRepository), _networkStatusService);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(AppResources.ErrorLoadFailed, viewModel.ErrorMessage);
        Assert.Empty(viewModel.SavedItems);
        Assert.False(viewModel.HasMore);
    }

    /// <summary>
    /// Verifies that LoadCommand waits for an in-flight LoadMoreCommand and reloads the first page,
    /// instead of leaving the stale next page in the cleared list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WhenLoadMoreInFlight_ReloadsFirstPage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        for (var i = 0; i < LaterViewModel.PageSize + 5; i++)
        {
            await _itemRepository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Saved {i:D2}",
                GuidOrHash = $"saved-{i}",
                IsRead = false,
                IsSavedForLater = true,
                PublishedAt = new DateTime(2026, 1, 1).AddDays(i),
            });
        }

        var gatedRepository = new GatedItemRepository(_itemRepository);
        var viewModel = new LaterViewModel(gatedRepository, _networkStatusService);

        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(LaterViewModel.PageSize, viewModel.SavedItems.Count);
        Assert.True(viewModel.HasMore);

        gatedRepository.GateNextPageRequest();
        var loadMoreTask = viewModel.LoadMoreCommand.ExecuteAsync(null);
        await TestWaitHelper.WaitUntilAsync(() => viewModel.IsLoading);

        var loadTask = viewModel.LoadCommand.ExecuteAsync(null);
        gatedRepository.ReleaseGate();

        await Task.WhenAll(loadMoreTask, loadTask);

        Assert.Equal(LaterViewModel.PageSize, viewModel.SavedItems.Count);
        Assert.True(viewModel.HasMore);
        Assert.Equal($"Saved {LaterViewModel.PageSize + 4:D2}", viewModel.SavedItems[0].Title);
    }

    /// <summary>
    /// An <see cref="IItemRepository"/> decorator that can block the next
    /// <see cref="IItemRepository.GetSavedForLaterAsync(int, int)"/> call on a gate.
    /// </summary>
    private sealed class GatedItemRepository : IItemRepository
    {
        private readonly IItemRepository _inner;
        private TaskCompletionSource? _gate;

        public GatedItemRepository(IItemRepository inner)
        {
            _inner = inner;
        }

        public void GateNextPageRequest()
        {
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void ReleaseGate()
        {
            var gate = _gate;
            _gate = null;
            gate?.TrySetResult();
        }

        public async Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize)
        {
            var gate = _gate;
            if (gate is not null)
            {
                await gate.Task;
            }

            return await _inner.GetSavedForLaterAsync(page, pageSize);
        }

        public Task<IReadOnlyList<Item>> GetAllAsync() => _inner.GetAllAsync();

        public Task<Item?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);

        public Task AddAsync(Item item) => _inner.AddAsync(item);

        public Task UpdateAsync(Item item) => _inner.UpdateAsync(item);

        public Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);

        public Task<IReadOnlyList<Item>> GetUnreadByDateAsync() => _inner.GetUnreadByDateAsync();

        public Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null) =>
            _inner.GetUnreadByDateAsync(page, pageSize, categoryId);

        public Task<int> GetUnreadCountAsync(Guid? categoryId = null) => _inner.GetUnreadCountAsync(categoryId);

        public Task MarkAllAsReadAsync(Guid? categoryId = null) => _inner.MarkAllAsReadAsync(categoryId);

        public Task ToggleSavedForLaterAsync(Guid id) => _inner.ToggleSavedForLaterAsync(id);

        public Task MarkAsReadAsync(Guid id) => _inner.MarkAsReadAsync(id);

        public Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId) => _inner.GetByFeedAsync(feedId);

        public Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId) => _inner.GetByCategoryAsync(categoryId);

        public Task AddRangeAsync(IReadOnlyList<Item> items) => _inner.AddRangeAsync(items);

        public Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
            _inner.DeleteExpiredAsync(cutoff, cancellationToken);

        public Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
            _inner.GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken);

        public Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
            _inner.DeleteRangeAsync(ids, cancellationToken);
    }

    /// <summary>
    /// An <see cref="IItemRepository"/> decorator that delegates every call to an inner
    /// repository but fails the saved-for-later query to simulate a load failure.
    /// </summary>
    private sealed class FailingItemRepository : IItemRepository
    {
        private readonly IItemRepository _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailingItemRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public FailingItemRepository(IItemRepository inner)
        {
            _inner = inner;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize)
        {
            return Task.FromException<IReadOnlyList<ItemListItem>>(new InvalidOperationException("Simulated load failure."));
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<Item>> GetAllAsync() => _inner.GetAllAsync();

        /// <inheritdoc />
        public Task<Item?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);

        /// <inheritdoc />
        public Task AddAsync(Item item) => _inner.AddAsync(item);

        /// <inheritdoc />
        public Task UpdateAsync(Item item) => _inner.UpdateAsync(item);

        /// <inheritdoc />
        public Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);

        /// <inheritdoc />
        public Task<IReadOnlyList<Item>> GetUnreadByDateAsync() => _inner.GetUnreadByDateAsync();

        /// <inheritdoc />
        public Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null) =>
            _inner.GetUnreadByDateAsync(page, pageSize, categoryId);

        /// <inheritdoc />
        public Task<int> GetUnreadCountAsync(Guid? categoryId = null) => _inner.GetUnreadCountAsync(categoryId);

        /// <inheritdoc />
        public Task MarkAllAsReadAsync(Guid? categoryId = null) => _inner.MarkAllAsReadAsync(categoryId);

        /// <inheritdoc />
        public Task ToggleSavedForLaterAsync(Guid id) => _inner.ToggleSavedForLaterAsync(id);

        /// <inheritdoc />
        public Task MarkAsReadAsync(Guid id) => _inner.MarkAsReadAsync(id);

        /// <inheritdoc />
        public Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId) => _inner.GetByFeedAsync(feedId);

        /// <inheritdoc />
        public Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId) => _inner.GetByCategoryAsync(categoryId);

        /// <inheritdoc />
        public Task AddRangeAsync(IReadOnlyList<Item> items) => _inner.AddRangeAsync(items);

        /// <inheritdoc />
        public Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
            _inner.DeleteExpiredAsync(cutoff, cancellationToken);

        /// <inheritdoc />
        public Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
            _inner.GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken);

        /// <inheritdoc />
        public Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
            _inner.DeleteRangeAsync(ids, cancellationToken);
    }
}
