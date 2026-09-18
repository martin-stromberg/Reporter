// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Data;

/// <summary>
/// Copies article contents from the legacy <c>items.content_html</c> column
/// into the content store. Runs before the EF migration that drops the
/// column; the <c>PRAGMA table_info</c> probe makes it a no-op once the
/// column is gone, so repeated calls are safe.
/// </summary>
public class ItemContentMigrationService : IContentMigrationService
{
    private const int PageSize = 500;

    private readonly IDbContextFactory<ReporterDbContext> _factory;
    private readonly IDbContextFactory<ContentDbContext> _contentFactory;
    private readonly IItemContentStore _contentStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemContentMigrationService"/> class.
    /// </summary>
    /// <param name="factory">The database context factory of the user database.</param>
    /// <param name="contentFactory">The database context factory of the content database.</param>
    /// <param name="contentStore">The store holding the item contents.</param>
    public ItemContentMigrationService(
        IDbContextFactory<ReporterDbContext> factory,
        IDbContextFactory<ContentDbContext> contentFactory,
        IItemContentStore contentStore)
    {
        _factory = factory;
        _contentFactory = contentFactory;
        _contentStore = contentStore;
    }

    /// <inheritdoc />
    public async Task MigrateLegacyContentAsync(CancellationToken cancellationToken = default)
    {
        // Das Content-Schema wird zuerst sichergestellt, damit ein erneuter
        // Aufruf (App.OnStart) auch dann funktioniert, wenn die Migration in
        // MauiProgram.MigrateContentStoreAndLegacyData scheiterte.
        await using (var contentContext = await _contentFactory.CreateDbContextAsync(cancellationToken))
        {
            await contentContext.Database.MigrateAsync(cancellationToken);
        }

        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            if (!await HasContentHtmlColumnAsync(connection, cancellationToken))
            {
                return;
            }

            var offset = 0;
            while (true)
            {
                var entries = await ReadPageAsync(connection, offset, cancellationToken);
                if (entries.Count == 0)
                {
                    break;
                }

                await _contentStore.SetRangeAsync(entries, cancellationToken);
                if (entries.Count < PageSize)
                {
                    break;
                }

                offset += PageSize;
            }
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> HasContentHtmlColumnAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('items')";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), "content_html", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<List<ItemContentEntry>> ReadPageAsync(DbConnection connection, int offset, CancellationToken cancellationToken)
    {
        var entries = new List<ItemContentEntry>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, content_html FROM items WHERE content_html IS NOT NULL ORDER BY id LIMIT $limit OFFSET $offset";
        var limit = command.CreateParameter();
        limit.ParameterName = "$limit";
        limit.Value = PageSize;
        command.Parameters.Add(limit);
        var offsetParameter = command.CreateParameter();
        offsetParameter.ParameterName = "$offset";
        offsetParameter.Value = offset;
        command.Parameters.Add(offsetParameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var itemId = Guid.Parse(reader.GetString(0));
            var contentHtml = reader.IsDBNull(1) ? null : reader.GetString(1);
            entries.Add(new ItemContentEntry(itemId, contentHtml));
        }

        return entries;
    }
}
