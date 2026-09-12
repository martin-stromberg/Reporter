// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;
using Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains persistence and query tests for the <see cref="ReporterDbContext"/> class.
/// </summary>
public class ReporterDbContextTests_Persistence
{
    /// <summary>
    /// Verifies that SaveChanges persists a feed together with its category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveChangesAsync_PersistsFeedWithCategory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var category = new Category { Name = "News" };
        context.Categories.Add(category);

        var feed = new Feed
        {
            Url = "https://example.com/feed",
            Title = "Example Feed",
            CategoryId = category.Id,
        };
        context.Feeds.Add(feed);
        await context.SaveChangesAsync();

        var savedFeed = await context.Feeds
            .AsNoTracking()
            .FirstAsync(f => f.Id == feed.Id);

        Assert.Equal("https://example.com/feed", savedFeed.Url);
        Assert.Equal("Example Feed", savedFeed.Title);
        Assert.Equal(category.Id, savedFeed.CategoryId);
    }

    /// <summary>
    /// Verifies that Include loads an item together with its feed and category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Items_WithInclude_ReturnsFeedAndCategory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var category = new Category { Name = "News" };
        context.Categories.Add(category);

        var feed = new Feed
        {
            Url = "https://example.com/feed",
            Title = "Example Feed",
            CategoryId = category.Id,
        };
        context.Feeds.Add(feed);
        await context.SaveChangesAsync();

        var item = new Item
        {
            FeedId = feed.Id,
            Title = "Sample Item",
            Link = "https://example.com/item",
            GuidOrHash = "abc123",
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var savedItem = await context.Items
            .AsNoTracking()
            .Include(i => i.Feed)
            .ThenInclude(f => f.Category)
            .FirstAsync();

        Assert.Equal("Sample Item", savedItem.Title);
        Assert.NotNull(savedItem.Feed);
        Assert.Equal("Example Feed", savedItem.Feed.Title);
        Assert.NotNull(savedItem.Feed.Category);
        Assert.Equal("News", savedItem.Feed.Category.Name);
    }
}
