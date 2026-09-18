// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;
using Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Contains schema and persistence tests for the <see cref="ContentDbContext"/> class.
/// </summary>
public class ContentDbContextTests
{
    /// <summary>
    /// Verifies that the <see cref="ItemContent"/> entity is mapped to the
    /// expected table and columns with <c>item_id</c> as the primary key.
    /// </summary>
    [Fact]
    public void ItemContents_MappedToExpectedTable()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new ContentDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(ItemContent));
        Assert.NotNull(entityType);
        Assert.Equal("item_contents", entityType.GetTableName());
        Assert.Equal("item_id", entityType.FindProperty(nameof(ItemContent.ItemId))?.GetColumnName());
        Assert.Equal("content_html", entityType.FindProperty(nameof(ItemContent.ContentHtml))?.GetColumnName());
        var key = Assert.Single(entityType.GetKeys());
        Assert.Equal(nameof(ItemContent.ItemId), Assert.Single(key.Properties).Name);
    }

    /// <summary>
    /// Verifies that an <see cref="ItemContent"/> entity survives a persist
    /// roundtrip.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ItemContent_PersistRoundtrip()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseSqlite(connection)
            .Options;

        var itemId = Guid.NewGuid();
        await using (var context = new ContentDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.ItemContents.Add(new ItemContent { ItemId = itemId, ContentHtml = "<p>body</p>" });
            await context.SaveChangesAsync();
        }

        await using (var context = new ContentDbContext(options))
        {
            var reloaded = await context.ItemContents.AsNoTracking().SingleAsync();
            Assert.Equal(itemId, reloaded.ItemId);
            Assert.Equal("<p>body</p>", reloaded.ContentHtml);
        }
    }
}
