// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;

namespace Reporter.Data;

/// <summary>
/// Runs the full database migration pass in the mandatory order: the legacy
/// <c>items.content_html</c> values are copied into the content store before
/// the <c>reporter.db</c> schema migration drops that column. A failure of the
/// content copy propagates and thereby blocks the schema migration instead of
/// silently losing the stored contents.
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IContentMigrationService _contentMigrationService;
    private readonly IDbContextFactory<ReporterDbContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMigrationService"/> class.
    /// </summary>
    /// <param name="contentMigrationService">The service migrating the content store schema and the legacy content.</param>
    /// <param name="factory">The database context factory of the user database.</param>
    public DatabaseMigrationService(
        IContentMigrationService contentMigrationService,
        IDbContextFactory<ReporterDbContext> factory)
    {
        _contentMigrationService = contentMigrationService;
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // Reihenfolge-Invariante: Der Legacy-Content muss kopiert sein, bevor
        // die reporter.db-Migration items.content_html loescht. Der Dienst
        // migriert dabei auch das Schema der Content-Datenbank. Ein Fehler
        // der Kopie darf die Schema-Migration nicht ausloesen.
        await _contentMigrationService.MigrateLegacyContentAsync(cancellationToken).ConfigureAwait(false);

        await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
