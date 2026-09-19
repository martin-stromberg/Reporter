// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="FeedRepository"/> class.
/// </summary>
public class FeedRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeItemContentStore _contentStore;
    private readonly FeedRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedRepositoryTests"/> class.
    /// </summary>
    public FeedRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _contentStore = new FakeItemContentStore();
        _repository = new FeedRepository(_factory, _contentStore);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a feed can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsFeed()
    {
        var feed = new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/feed",
            Title = "Example Feed",
            NotificationsEnabled = true,
        };
        await _repository.AddAsync(feed);

        var result = await _repository.GetByIdAsync(feed.Id);

        Assert.NotNull(result);
        Assert.Equal("https://example.com/feed", result.Url);
        Assert.Equal("Example Feed", result.Title);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns all added feeds ordered by title.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ReturnsFeedsOrderedByTitle()
    {
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://b.com", Title = "B", NotificationsEnabled = true });
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://a.com", Title = "A", NotificationsEnabled = true });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("B", result[1].Title);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://old.com", Title = "Old", NotificationsEnabled = true };
        await _repository.AddAsync(feed);

        await _repository.UpdateAsync(new Feed
        {
            Id = feed.Id,
            Url = "https://new.com",
            Title = "New",
            NotificationsEnabled = false,
        });
        var result = await _repository.GetByIdAsync(feed.Id);

        Assert.NotNull(result);
        Assert.Equal("https://new.com", result.Url);
        Assert.False(result.NotificationsEnabled);
        Assert.Equal("New", result.Title);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesFeed()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://delete.com", Title = "ToDelete", NotificationsEnabled = true };
        await _repository.AddAsync(feed);

        await _repository.DeleteAsync(feed.Id);
        var result = await _repository.GetByIdAsync(feed.Id);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null for a non-existing id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetAllWithDetailsAsync projects the notifications flag onto the feed list item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllWithDetailsAsync_ProjectsNotificationsEnabled()
    {
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://a.com", Title = "A", NotificationsEnabled = false });
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://b.com", Title = "B", NotificationsEnabled = true });

        var result = await _repository.GetAllWithDetailsAsync();

        Assert.False(result.Single(f => f.Title == "A").NotificationsEnabled);
        Assert.True(result.Single(f => f.Title == "B").NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists the favicon URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsFaviconUrl()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://old.com", Title = "Old", NotificationsEnabled = true };
        await _repository.AddAsync(feed);

        await _repository.UpdateAsync(new Feed
        {
            Id = feed.Id,
            Url = "https://old.com",
            Title = "Old",
            NotificationsEnabled = true,
            FaviconUrl = "https://old.com/favicon.ico",
        });
        var result = await _repository.GetByIdAsync(feed.Id);

        Assert.NotNull(result);
        Assert.Equal("https://old.com/favicon.ico", result.FaviconUrl);
    }

    /// <summary>
    /// Verifies that GetAllWithDetailsAsync projects the favicon URL onto the feed list item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllWithDetailsAsync_ProjectsFaviconUrl()
    {
        await _repository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://a.com",
            Title = "A",
            NotificationsEnabled = true,
            FaviconUrl = "https://a.com/icon.png",
        });
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://b.com", Title = "B", NotificationsEnabled = true });

        var result = await _repository.GetAllWithDetailsAsync();

        Assert.Equal("https://a.com/icon.png", result.Single(f => f.Title == "A").FaviconUrl);
        Assert.Null(result.Single(f => f.Title == "B").FaviconUrl);
        Assert.Equal("A", result.Single(f => f.Title == "A").FeedInitial);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists the last sync error kind and message.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsLastError()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://old.com", Title = "Old", NotificationsEnabled = true };
        await _repository.AddAsync(feed);

        await _repository.UpdateAsync(new Feed
        {
            Id = feed.Id,
            Url = "https://old.com",
            Title = "Old",
            NotificationsEnabled = true,
            LastErrorKind = FeedSyncErrorKind.Network,
            LastErrorMessage = "Synchronization failed: No connection",
        });
        var result = await _repository.GetByIdAsync(feed.Id);

        Assert.NotNull(result);
        Assert.Equal(FeedSyncErrorKind.Network, result.LastErrorKind);
        Assert.Equal("Synchronization failed: No connection", result.LastErrorMessage);
    }

    /// <summary>
    /// Verifies that GetAllWithDetailsAsync projects the last sync error fields
    /// onto the feed list item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllWithDetailsAsync_ProjectsLastError()
    {
        await _repository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://a.com",
            Title = "A",
            NotificationsEnabled = true,
            LastErrorKind = FeedSyncErrorKind.Parse,
            LastErrorMessage = "Synchronization failed: invalid xml",
        });
        await _repository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = "https://b.com", Title = "B", NotificationsEnabled = true });

        var result = await _repository.GetAllWithDetailsAsync();

        var feedA = result.Single(f => f.Title == "A");
        Assert.Equal(FeedSyncErrorKind.Parse, feedA.LastErrorKind);
        Assert.Equal("Synchronization failed: invalid xml", feedA.LastErrorMessage);
        var feedB = result.Single(f => f.Title == "B");
        Assert.Null(feedB.LastErrorKind);
        Assert.Null(feedB.LastErrorMessage);
    }

    /// <summary>
    /// Verifies that deleting a feed also deletes its saved items via cascade delete.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_CascadeDeletesSavedItems()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Feed", NotificationsEnabled = true };
        await _repository.AddAsync(feed);
        var itemRepository = new ItemRepository(_factory, _contentStore);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feed.Id,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = true,
            IsSavedForLater = true,
        };
        await itemRepository.AddAsync(item);

        await _repository.DeleteAsync(feed.Id);
        var result = await itemRepository.GetByIdAsync(item.Id);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that deleting a feed also removes the stored contents of its
    /// items: the item rows fall to the database cascade, but the content rows
    /// live in the separate content database and must be deleted explicitly.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesItemContents()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Feed", NotificationsEnabled = true };
        await _repository.AddAsync(feed);
        var itemRepository = new ItemRepository(_factory, _contentStore);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feed.Id,
            Title = "Item",
            GuidOrHash = "hash",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>body</p>",
        };
        await itemRepository.AddAsync(item);

        await _repository.DeleteAsync(feed.Id);

        Assert.Null(await itemRepository.GetByIdAsync(item.Id));
        Assert.Null(await _contentStore.GetAsync(item.Id));
        Assert.Empty(await _contentStore.GetItemIdsAsync());
    }

    /// <summary>
    /// Verifies that deleting a feed also removes the stored images of its
    /// items: the content rows — including the image columns — live in the
    /// separate content database and must be deleted explicitly.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesItemImages()
    {
        var feed = new Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Feed", NotificationsEnabled = true };
        await _repository.AddAsync(feed);
        var itemRepository = new ItemRepository(_factory, _contentStore);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feed.Id,
            Title = "Item",
            GuidOrHash = "hash",
            IsRead = false,
            IsSavedForLater = false,
            Image = new ItemImage([1], "image/png", null),
        };
        await itemRepository.AddAsync(item);

        await _repository.DeleteAsync(feed.Id);

        Assert.Null(await _contentStore.GetImageAsync(item.Id));
        Assert.Empty(await _contentStore.GetImageIdsAsync(new List<Guid> { item.Id }));
    }
}
