// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Data;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ItemContentMigrationService"/> class.
/// </summary>
public class ItemContentMigrationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly TestContentDbContextFactory _contentFactory;
    private readonly ItemContentRepository _contentStore;
    private readonly ItemContentMigrationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemContentMigrationServiceTests"/> class.
    /// </summary>
    public ItemContentMigrationServiceTests()
    {
        _factory = new TestDbContextFactory();
        _contentFactory = new TestContentDbContextFactory();
        _contentStore = new ItemContentRepository(_contentFactory);
        _service = new ItemContentMigrationService(_factory, _contentFactory, _contentStore);
    }

    /// <summary>
    /// Disposes the test factories.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
        _contentFactory.Dispose();
    }

    /// <summary>
    /// Verifies that legacy <c>items.content_html</c> values are copied into
    /// the content store.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateLegacyContentAsync_CopiesContentHtml()
    {
        var itemId = Guid.NewGuid();
        var emptyId = Guid.NewGuid();
        await SeedLegacyItemsAsync((itemId, "<p>legacy</p>"), (emptyId, null));

        await _service.MigrateLegacyContentAsync();

        Assert.Equal("<p>legacy</p>", await _contentStore.GetAsync(itemId));
        Assert.Null(await _contentStore.GetAsync(emptyId));
    }

    /// <summary>
    /// Verifies that a second run of the migration has no additional effect:
    /// the copy is idempotent.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateLegacyContentAsync_Idempotent()
    {
        var itemId = Guid.NewGuid();
        await SeedLegacyItemsAsync((itemId, "<p>legacy</p>"));

        await _service.MigrateLegacyContentAsync();
        await _service.MigrateLegacyContentAsync();

        Assert.Equal("<p>legacy</p>", await _contentStore.GetAsync(itemId));
        Assert.Single(await _contentStore.GetItemIdsAsync());
    }

    /// <summary>
    /// Verifies that more rows than one page (500) are all copied across the
    /// paged reads: with 501 legacy rows the migration must fetch the second
    /// page instead of stopping after the first batch.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateLegacyContentAsync_MoreThanOnePage_CopiesAll()
    {
        var rows = Enumerable.Range(0, 501)
            .Select(i => (Guid.NewGuid(), (string?)$"<p>{i}</p>"))
            .ToArray();
        await SeedLegacyItemsAsync(rows);

        await _service.MigrateLegacyContentAsync();

        var storedIds = await _contentStore.GetItemIdsAsync();
        Assert.Equal(rows.Length, storedIds.Count);
        Assert.All(rows, row => Assert.Contains(row.Item1, storedIds));
    }

    /// <summary>
    /// Verifies that the migration is a no-op when the legacy
    /// <c>content_html</c> column no longer exists (the EF migration already
    /// dropped it).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateLegacyContentAsync_NoColumn_NoOp()
    {
        // Die Testdatenbank wird mit dem neuen Schema (ohne content_html)
        // angelegt — die Spaltenpruefung muss die Migration ueberspringen.
        await _service.MigrateLegacyContentAsync();

        Assert.Empty(await _contentStore.GetItemIdsAsync());
    }

    /// <summary>
    /// Simulates a pre-migration database: adds the legacy
    /// <c>content_html</c> column back to <c>items</c> and writes the supplied
    /// rows with raw SQL.
    /// </summary>
    /// <param name="rows">The legacy item rows to insert (item ID, HTML content).</param>
    private async Task SeedLegacyItemsAsync(params (Guid, string?)[] rows)
    {
        var feedId = Guid.NewGuid();
        await using (var context = _factory.CreateDbContext())
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await ExecuteAsync(
                connection,
                "INSERT INTO feeds (id, url, title, notifications_enabled) " +
                $"VALUES ('{feedId}', 'https://example.com/feed', 'Feed', 1)");
            await ExecuteAsync(connection, "ALTER TABLE items ADD COLUMN content_html TEXT");
            foreach (var (itemId, contentHtml) in rows)
            {
                await ExecuteAsync(
                    connection,
                    "INSERT INTO items (id, feed_id, title, guid_or_hash, is_read, is_saved_for_later, content_html) " +
                    $"VALUES ('{itemId}', '{feedId}', 'Title', 'hash-{itemId}', 0, 0, {(contentHtml is null ? "NULL" : $"'{contentHtml}'")})");
            }
        }
    }

    private static async Task ExecuteAsync(System.Data.Common.DbConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}
