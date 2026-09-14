// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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
        var debugLogCount = await context.DebugLogEntries.CountAsync();

        Assert.Equal(0, feedCount);
        Assert.Equal(0, categoryCount);
        Assert.Equal(0, itemCount);
        Assert.Equal(0, keywordCount);
        Assert.Equal(1, settingsCount);
        Assert.Equal(0, syncLogCount);
        Assert.Equal(0, debugLogCount);
    }

    /// <summary>
    /// Verifies that the debug log entity is mapped to the expected table, columns and index.
    /// </summary>
    [Fact]
    public void DebugLogEntries_MappedToExpectedTable()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new ReporterDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Reporter.Data.Entities.DebugLogEntry));
        Assert.NotNull(entityType);
        Assert.Equal("debug_log_entries", entityType.GetTableName());
        Assert.Equal("id", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Id))?.GetColumnName());
        Assert.Equal("timestamp", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Timestamp))?.GetColumnName());
        Assert.Equal("level", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Level))?.GetColumnName());
        Assert.Equal("category", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Category))?.GetColumnName());
        Assert.Equal("message", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Message))?.GetColumnName());
        Assert.Equal("details", entityType.FindProperty(nameof(Reporter.Data.Entities.DebugLogEntry.Details))?.GetColumnName());
        Assert.Contains(entityType.GetIndexes(), i => i.Properties.Any(p => p.Name == nameof(Reporter.Data.Entities.DebugLogEntry.Timestamp)));

        var settingsType = context.Model.FindEntityType(typeof(Reporter.Data.Entities.Settings));
        var debugProperty = settingsType?.FindProperty(nameof(Reporter.Data.Entities.Settings.DebugCollectionEnabled));
        Assert.NotNull(debugProperty);
        Assert.Equal("debug_collection_enabled", debugProperty.GetColumnName());
        Assert.Equal(false, debugProperty.GetDefaultValue());
    }
}
