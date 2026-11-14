// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Brings both databases to their current schema in a single pass: first the
/// content store schema and the legacy <c>items.content_html</c> copy, then the
/// <c>reporter.db</c> schema. The order is mandatory — the <c>reporter.db</c>
/// migration drops the legacy column, so running it before the copy would lose
/// the stored article contents. Encapsulating the sequence here keeps callers
/// from having to reproduce it.
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// Migrates the content database schema and copies legacy
    /// <c>items.content_html</c> values into the content store, then migrates
    /// the <c>reporter.db</c> schema. When the legacy copy fails, the schema
    /// migration is not attempted so the column is not dropped before its
    /// values are preserved.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
