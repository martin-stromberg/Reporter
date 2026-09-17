// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Core.Services;
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

    /// <summary>
    /// Verifies that the debug collection switch on the singleton settings record
    /// survives a persist roundtrip.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Settings_DebugCollectionEnabled_PersistRoundtrip()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var settings = await context.Settings.FirstAsync();
        Assert.False(settings.DebugCollectionEnabled);

        settings.DebugCollectionEnabled = true;
        await context.SaveChangesAsync();

        var reloaded = await context.Settings.AsNoTracking().FirstAsync();
        Assert.True(reloaded.DebugCollectionEnabled);
    }

    /// <summary>
    /// Verifies that a debug log entry survives a persist roundtrip with all fields.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DebugLogEntry_PersistRoundtrip()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var timestamp = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        var entry = new DebugLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp,
            Level = "Error",
            Category = "Exception",
            Message = "unhandled exception",
            Details = "stacktrace",
        };
        context.DebugLogEntries.Add(entry);
        await context.SaveChangesAsync();

        var reloaded = await context.DebugLogEntries.AsNoTracking().FirstAsync();

        Assert.Equal(entry.Id, reloaded.Id);
        Assert.Equal(timestamp, reloaded.Timestamp);
        Assert.Equal("Error", reloaded.Level);
        Assert.Equal("Exception", reloaded.Category);
        Assert.Equal("unhandled exception", reloaded.Message);
        Assert.Equal("stacktrace", reloaded.Details);
    }

    /// <summary>
    /// Verifies that the last sync error columns of a feed survive a persist
    /// roundtrip.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Feed_PersistRoundtrip_LastError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var feed = new Feed
        {
            Url = "https://example.com/feed",
            Title = "Example Feed",
            LastErrorKind = FeedSyncErrorKind.Network,
            LastErrorMessage = "Synchronization failed: No connection",
        };
        context.Feeds.Add(feed);
        await context.SaveChangesAsync();

        var reloaded = await context.Feeds.AsNoTracking().FirstAsync(f => f.Id == feed.Id);

        Assert.Equal(FeedSyncErrorKind.Network, reloaded.LastErrorKind);
        Assert.Equal("Synchronization failed: No connection", reloaded.LastErrorMessage);
    }
}
