using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;
using Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ReporterDbContext"/> class.
/// </summary>
public class ReporterDbContextTests
{
    /// <summary>
    /// Verifies that the database schema can be created in an in-memory SQLite database
    /// and that entities can be persisted and queried.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CanCreateSchemaAndPersistEntities()
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
        await context.SaveChangesAsync();

        var feed = new Feed { Url = "https://example.com/feed", Title = "Example Feed", CategoryId = category.Id };
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

        var itemFromDb = await context.Items
            .Include(i => i.Feed)
            .ThenInclude(f => f.Category)
            .FirstAsync();

        Assert.Equal("Sample Item", itemFromDb.Title);
        Assert.NotNull(itemFromDb.Feed);
        Assert.Equal("Example Feed", itemFromDb.Feed.Title);
        Assert.NotNull(itemFromDb.Feed.Category);
        Assert.Equal("News", itemFromDb.Feed.Category.Name);
    }
}
