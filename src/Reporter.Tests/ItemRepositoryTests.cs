using Reporter.Core.Models;
using Reporter.Data.Repositories;
using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ItemRepository"/> class.
/// </summary>
public class ItemRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ItemRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepositoryTests"/> class.
    /// </summary>
    public ItemRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new ItemRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<Guid> SeedFeedAsync()
    {
        var feedId = Guid.NewGuid();
        await using var context = _factory.CreateDbContext();
        context.Feeds.Add(new Entities.Feed
        {
            Id = feedId,
            Url = "https://example.com/feed",
            Title = "Example Feed",
        });
        await context.SaveChangesAsync();
        return feedId;
    }

    /// <summary>
    /// Verifies that an item can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsItem()
    {
        var feedId = await SeedFeedAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Sample Item",
            GuidOrHash = "abc123",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(item);

        var result = await _repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Sample Item", result.Title);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns all items ordered by PublishedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ReturnsItemsOrderedByPublishedAtDescending()
    {
        var feedId = await SeedFeedAsync();
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Older",
            GuidOrHash = "old",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Newer",
            GuidOrHash = "new",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 2),
        });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Newer", result[0].Title);
        Assert.Equal("Older", result[1].Title);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var feedId = await SeedFeedAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Old",
            GuidOrHash = "hash",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(item);

        await _repository.UpdateAsync(new Item
        {
            Id = item.Id,
            FeedId = feedId,
            Title = "New",
            GuidOrHash = "hash",
            IsRead = true,
            IsSavedForLater = true,
            ReadAt = new DateTime(2026, 1, 2),
        });
        var result = await _repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("New", result.Title);
        Assert.True(result.IsRead);
        Assert.True(result.IsSavedForLater);
        Assert.Equal(new DateTime(2026, 1, 2), result.ReadAt);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the item.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var feedId = await SeedFeedAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToDelete",
            GuidOrHash = "delete",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(item);

        await _repository.DeleteAsync(item.Id);
        var result = await _repository.GetByIdAsync(item.Id);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetUnreadByDateAsync returns only unread items sorted by PublishedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending()
    {
        var feedId = await SeedFeedAsync();
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Read",
            GuidOrHash = "read",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 3),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread Newer",
            GuidOrHash = "unread-newer",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 2),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread Older",
            GuidOrHash = "unread-older",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = new DateTime(2026, 1, 1),
        });

        var result = await _repository.GetUnreadByDateAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Unread Newer", result[0].Title);
        Assert.Equal("Unread Older", result[1].Title);
    }

    /// <summary>
    /// Verifies that GetByFeedAsync returns only items for the specified feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByFeedAsync_ReturnsOnlyMatchingFeed()
    {
        var feedA = await SeedFeedAsync();
        var feedBId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Feeds.Add(new Entities.Feed
            {
                Id = feedBId,
                Url = "https://b.com",
                Title = "Feed B",
            });
            await context.SaveChangesAsync();
        }

        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedA,
            Title = "A",
            GuidOrHash = "a",
            IsRead = false,
            IsSavedForLater = false,
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedBId,
            Title = "B",
            GuidOrHash = "b",
            IsRead = false,
            IsSavedForLater = false,
        });

        var result = await _repository.GetByFeedAsync(feedA);

        Assert.Single(result);
        Assert.Equal("A", result[0].Title);
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync returns only items from feeds in the specified category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByCategoryAsync_ReturnsOnlyMatchingCategory()
    {
        var categoryId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            context.Categories.Add(new Entities.Category { Id = categoryId, Name = "News" });
            await context.SaveChangesAsync();
        }

        var feedId = await SeedFeedAsync();
        await using (var context = _factory.CreateDbContext())
        {
            var feed = await context.Feeds.FindAsync(feedId);
            if (feed is not null)
            {
                feed.CategoryId = categoryId;
                await context.SaveChangesAsync();
            }
        }

        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "News Item",
            GuidOrHash = "news",
            IsRead = false,
            IsSavedForLater = false,
        });

        var result = await _repository.GetByCategoryAsync(categoryId);

        Assert.Single(result);
        Assert.Equal("News Item", result[0].Title);
    }

    /// <summary>
    /// Verifies that GetSavedForLaterAsync returns only saved items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSavedForLaterAsync_ReturnsOnlySaved()
    {
        var feedId = await SeedFeedAsync();
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Not Saved",
            GuidOrHash = "notsaved",
            IsRead = false,
            IsSavedForLater = false,
        });

        var result = await _repository.GetSavedForLaterAsync();

        Assert.Single(result);
        Assert.Equal("Saved", result[0].Title);
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync returns an empty list when no feed matches the category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByCategoryAsync_NoFeedsInCategory_ReturnsEmpty()
    {
        var result = await _repository.GetByCategoryAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GetByFeedAsync returns an empty list when the feed has no items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByFeedAsync_NonExistingFeed_ReturnsEmpty()
    {
        var result = await _repository.GetByFeedAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GetUnreadByDateAsync returns an empty list when all items are read.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_AllRead_ReturnsEmpty()
    {
        var feedId = await SeedFeedAsync();
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Read",
            GuidOrHash = "read",
            IsRead = true,
            IsSavedForLater = false,
        });

        var result = await _repository.GetUnreadByDateAsync();

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GetUnreadByDateAsync returns a paged list of unread items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_Paged_ReturnsPage()
    {
        var feedId = await SeedFeedAsync();
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Item {i}",
                GuidOrHash = $"item-{i}",
                IsRead = false,
                IsSavedForLater = false,
                PublishedAt = new DateTime(2026, 1, 1).AddDays(i),
            });
        }

        var result = await _repository.GetUnreadByDateAsync(0, 2, null);

        Assert.Equal(2, result.Count);
        Assert.Equal("Item 4", result[0].Title);
    }

    /// <summary>
    /// Verifies that GetUnreadCountAsync returns the number of unread items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        var feedId = await SeedFeedAsync();
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread",
            GuidOrHash = "unread",
            IsRead = false,
            IsSavedForLater = false,
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Read",
            GuidOrHash = "read",
            IsRead = true,
            IsSavedForLater = false,
        });

        var result = await _repository.GetUnreadCountAsync();

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Verifies that MarkAsReadAsync sets the item as read.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MarkAsReadAsync_SetsIsRead()
    {
        var feedId = await SeedFeedAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToRead",
            GuidOrHash = "toread",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(item);

        await _repository.MarkAsReadAsync(item.Id);
        var result = await _repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.True(result.IsRead);
    }

    /// <summary>
    /// Verifies that ToggleSavedForLaterAsync toggles the saved state.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedForLaterAsync_TogglesState()
    {
        var feedId = await SeedFeedAsync();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToToggle",
            GuidOrHash = "totoggle",
            IsRead = false,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(item);

        await _repository.ToggleSavedForLaterAsync(item.Id);
        var result = await _repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.True(result.IsSavedForLater);
    }
}
