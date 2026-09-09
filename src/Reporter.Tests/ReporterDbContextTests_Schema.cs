using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Contains schema-related tests for the <see cref="ReporterDbContext"/> class.
/// </summary>
public class ReporterDbContextTests_Schema
{
    /// <summary>
    /// Verifies that EnsureCreated creates the database tables and makes DbSets queryable.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureCreatedAsync_CreatesQueryableTables()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ReporterDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var feedCount = await context.Feeds.CountAsync();
        var categoryCount = await context.Categories.CountAsync();
        var itemCount = await context.Items.CountAsync();
        var keywordCount = await context.Keywords.CountAsync();
        var settingsCount = await context.Settings.CountAsync();
        var syncLogCount = await context.SyncLogs.CountAsync();

        Assert.Equal(0, feedCount);
        Assert.Equal(0, categoryCount);
        Assert.Equal(0, itemCount);
        Assert.Equal(0, keywordCount);
        Assert.Equal(1, settingsCount);
        Assert.Equal(0, syncLogCount);
    }
}
