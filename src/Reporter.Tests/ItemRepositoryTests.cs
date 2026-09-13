// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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

    /// <summary>
    /// Verifies that an item can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsItem()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedA = await TestDataSeeder.SeedFeedAsync(_factory);
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

        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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

        var result = await _repository.GetSavedForLaterAsync(0, 20);

        Assert.Single(result);
        Assert.Equal("Saved", result[0].Title);
    }

    /// <summary>
    /// Verifies that GetSavedForLaterAsync returns the requested page of saved items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSavedForLaterAsync_Paged_ReturnsPage()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = $"Saved {i}",
                GuidOrHash = $"saved-{i}",
                IsRead = false,
                IsSavedForLater = true,
                PublishedAt = new DateTime(2026, 1, 1).AddDays(i),
            });
        }

        var firstPage = await _repository.GetSavedForLaterAsync(0, 2);
        var secondPage = await _repository.GetSavedForLaterAsync(1, 2);
        var thirdPage = await _repository.GetSavedForLaterAsync(2, 2);

        Assert.Equal(2, firstPage.Count);
        Assert.Equal("Saved 4", firstPage[0].Title);
        Assert.Equal("Saved 3", firstPage[1].Title);
        Assert.Equal(2, secondPage.Count);
        Assert.Equal("Saved 2", secondPage[0].Title);
        Assert.Equal("Saved 1", secondPage[1].Title);
        Assert.Single(thirdPage);
        Assert.Equal("Saved 0", thirdPage[0].Title);
    }

    /// <summary>
    /// Verifies that GetSavedForLaterAsync projects a formatted reading time.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSavedForLaterAsync_ProjectsReadingTimeText()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = false,
            IsSavedForLater = true,
            ContentHtml = "<p>" + string.Join(' ', Enumerable.Repeat("word", 400)) + "</p>",
        });

        var result = await _repository.GetSavedForLaterAsync(0, 20);

        Assert.Single(result);
        Assert.False(string.IsNullOrEmpty(result[0].ReadingTimeText));
    }

    /// <summary>
    /// Verifies that GetUnreadByDateAsync projects a formatted reading time.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadByDateAsync_Paged_ProjectsReadingTimeText()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread",
            GuidOrHash = "unread",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>" + string.Join(' ', Enumerable.Repeat("word", 400)) + "</p>",
        });

        var result = await _repository.GetUnreadByDateAsync(0, 20);

        Assert.Single(result);
        Assert.False(string.IsNullOrEmpty(result[0].ReadingTimeText));
    }

    /// <summary>
    /// Verifies that AddRangeAsync persists all supplied items in one batch.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddRangeAsync_InsertsAllItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var items = new List<Item>
        {
            new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = "First",
                GuidOrHash = "first",
                IsRead = false,
                IsSavedForLater = false,
            },
            new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Title = "Second",
                GuidOrHash = "second",
                IsRead = false,
                IsSavedForLater = false,
            },
        };

        await _repository.AddRangeAsync(items);

        var persisted = await _repository.GetByFeedAsync(feedId);
        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, i => i.Title == "First");
        Assert.Contains(persisted, i => i.Title == "Second");
    }

    /// <summary>
    /// Verifies that AddRangeAsync with an empty list does nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddRangeAsync_EmptyList_DoesNothing()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);

        await _repository.AddRangeAsync(new List<Item>());

        Assert.Empty(await _repository.GetByFeedAsync(feedId));
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
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

    /// <summary>
    /// Verifies that DeleteExpiredAsync removes expired read items and returns the deleted count.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_RemovesExpiredReadItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var expired = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Expired",
            GuidOrHash = "expired",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        var withoutTimestamps = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "NoTimestamps",
            GuidOrHash = "notimestamps",
            IsRead = true,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(expired);
        await _repository.AddAsync(withoutTimestamps);

        var deleted = await _repository.DeleteExpiredAsync(cutoff);

        Assert.Equal(1, deleted);
        Assert.Null(await _repository.GetByIdAsync(expired.Id));
        Assert.NotNull(await _repository.GetByIdAsync(withoutTimestamps.Id));
    }

    /// <summary>
    /// Verifies that DeleteExpiredAsync never deletes items saved for later.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_KeepsSavedForLaterItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = true,
            IsSavedForLater = true,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        await _repository.AddAsync(item);

        var deleted = await _repository.DeleteExpiredAsync(cutoff);

        Assert.Equal(0, deleted);
        Assert.NotNull(await _repository.GetByIdAsync(item.Id));
    }

    /// <summary>
    /// Verifies that DeleteExpiredAsync keeps expired unread items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_KeepsUnreadItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread",
            GuidOrHash = "unread",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
        };
        await _repository.AddAsync(item);

        var deleted = await _repository.DeleteExpiredAsync(cutoff);

        Assert.Equal(0, deleted);
        Assert.NotNull(await _repository.GetByIdAsync(item.Id));
    }

    /// <summary>
    /// Verifies that DeleteExpiredAsync keeps read items within the retention period.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_KeepsNonExpiredItems()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Recent",
            GuidOrHash = "recent",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-10),
            ReadAt = DateTime.UtcNow.AddDays(-5),
        };
        await _repository.AddAsync(item);

        var deleted = await _repository.DeleteExpiredAsync(cutoff);

        Assert.Equal(0, deleted);
        Assert.NotNull(await _repository.GetByIdAsync(item.Id));
    }

    /// <summary>
    /// Verifies that DeleteExpiredAsync uses ReadAt over PublishedAt as the expiration timestamp.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_UsesReadAtOverPublishedAt()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "OldButRecentlyRead",
            GuidOrHash = "oldrecent",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-100),
            ReadAt = DateTime.UtcNow.AddDays(-5),
        };
        await _repository.AddAsync(item);

        var deleted = await _repository.DeleteExpiredAsync(cutoff);

        Assert.Equal(0, deleted);
        Assert.NotNull(await _repository.GetByIdAsync(item.Id));
    }

    /// <summary>
    /// Verifies that GetSavedForLaterAsync returns saved items ordered by PublishedAt descending.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSavedForLaterAsync_OrdersByPublishedAtDescending()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Older",
            GuidOrHash = "older",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 1),
        });
        await _repository.AddAsync(new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Newer",
            GuidOrHash = "newer",
            IsRead = false,
            IsSavedForLater = true,
            PublishedAt = new DateTime(2026, 1, 2),
        });

        var result = await _repository.GetSavedForLaterAsync(0, 20);

        Assert.Equal(2, result.Count);
        Assert.Equal("Newer", result[0].Title);
        Assert.Equal("Older", result[1].Title);
    }

    /// <summary>
    /// Verifies that ToggleSavedForLaterAsync toggles the saved state back to false.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleSavedForLaterAsync_TogglesBackToFalse()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ToUntoggle",
            GuidOrHash = "tountoggle",
            IsRead = false,
            IsSavedForLater = true,
        };
        await _repository.AddAsync(item);

        await _repository.ToggleSavedForLaterAsync(item.Id);
        var result = await _repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.False(result.IsSavedForLater);
    }

    /// <summary>
    /// Verifies that DeleteExpiredAsync propagates cancellation to the database operation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteExpiredAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _repository.DeleteExpiredAsync(DateTime.UtcNow, cts.Token));
    }

    /// <summary>
    /// Verifies that GetExpiredKeywordCandidatesAsync returns read, unsaved items whose PublishedAt is older than the cutoff.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetExpiredKeywordCandidatesAsync_UsesPublishedAtOverReadAt()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var oldPublishedRecentlyRead = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "OldPublishedRecentlyRead",
            GuidOrHash = "old-pub",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-1),
        };
        var recentPublishedOldRead = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "RecentPublishedOldRead",
            GuidOrHash = "recent-pub",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-5),
            ReadAt = DateTime.UtcNow.AddDays(-60),
        };
        await _repository.AddAsync(oldPublishedRecentlyRead);
        await _repository.AddAsync(recentPublishedOldRead);

        var candidates = await _repository.GetExpiredKeywordCandidatesAsync(cutoff);

        Assert.Single(candidates);
        Assert.Equal(oldPublishedRecentlyRead.Id, candidates[0].Id);
    }

    /// <summary>
    /// Verifies that GetExpiredKeywordCandidatesAsync keeps unread and saved-for-later items out of the result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetExpiredKeywordCandidatesAsync_KeepsUnreadAndSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var unread = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread",
            GuidOrHash = "unread",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
        };
        var saved = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Saved",
            GuidOrHash = "saved",
            IsRead = true,
            IsSavedForLater = true,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        var eligible = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Eligible",
            GuidOrHash = "eligible",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
        };
        await _repository.AddAsync(unread);
        await _repository.AddAsync(saved);
        await _repository.AddAsync(eligible);

        var candidates = await _repository.GetExpiredKeywordCandidatesAsync(cutoff);

        Assert.Single(candidates);
        Assert.Equal(eligible.Id, candidates[0].Id);
    }

    /// <summary>
    /// Verifies that GetExpiredKeywordCandidatesAsync falls back to ReadAt and keeps items without timestamps.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetExpiredKeywordCandidatesAsync_FallsBackToReadAt()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var readAtOnly = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "ReadAtOnly",
            GuidOrHash = "readat-only",
            IsRead = true,
            IsSavedForLater = false,
            ReadAt = DateTime.UtcNow.AddDays(-60),
        };
        var noTimestamps = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "NoTimestamps",
            GuidOrHash = "no-timestamps",
            IsRead = true,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(readAtOnly);
        await _repository.AddAsync(noTimestamps);

        var candidates = await _repository.GetExpiredKeywordCandidatesAsync(cutoff);

        Assert.Single(candidates);
        Assert.Equal(readAtOnly.Id, candidates[0].Id);
    }

    /// <summary>
    /// Verifies that DeleteRangeAsync deletes only the items with the given identifiers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteRangeAsync_DeletesOnlyGivenIds()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var first = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "First",
            GuidOrHash = "first",
            IsRead = true,
            IsSavedForLater = false,
        };
        var second = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Second",
            GuidOrHash = "second",
            IsRead = true,
            IsSavedForLater = false,
        };
        var kept = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Kept",
            GuidOrHash = "kept",
            IsRead = true,
            IsSavedForLater = false,
        };
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);
        await _repository.AddAsync(kept);

        var deleted = await _repository.DeleteRangeAsync(new List<Guid> { first.Id, second.Id });

        Assert.Equal(2, deleted);
        Assert.Null(await _repository.GetByIdAsync(first.Id));
        Assert.Null(await _repository.GetByIdAsync(second.Id));
        Assert.NotNull(await _repository.GetByIdAsync(kept.Id));
    }

    /// <summary>
    /// Verifies that DeleteRangeAsync returns zero for an empty identifier list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteRangeAsync_EmptyList_ReturnsZero()
    {
        var deleted = await _repository.DeleteRangeAsync(new List<Guid>());

        Assert.Equal(0, deleted);
    }
}
