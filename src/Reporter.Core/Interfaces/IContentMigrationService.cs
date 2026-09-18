// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Migrates article contents stored in the legacy <c>items.content_html</c>
/// column of the user database into the separate content store
/// (<c>item_contents</c> in <c>reporter-content.db</c>). The migration is
/// idempotent: once the column is dropped by the EF migration, or when it
/// never existed, the migration is a no-op.
/// </summary>
public interface IContentMigrationService
{
    /// <summary>
    /// Copies the legacy <c>items.content_html</c> values into the content store asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MigrateLegacyContentAsync(CancellationToken cancellationToken = default);
}
