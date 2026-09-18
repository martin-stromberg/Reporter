// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Computes the file path sets for the cloud backup handling: the content
/// database (<c>reporter-content.db</c>) holds only re-downloadable article
/// contents and stays excluded, while the user database
/// (<c>reporter.db</c>) is included so subscriptions, settings and reading
/// state survive a device restore. Each database is addressed together with
/// its SQLite <c>-wal</c>/<c>-shm</c> sidecar files.
/// </summary>
public static class BackupExclusionPlan
{
    /// <summary>
    /// Gets the paths that must be excluded from the cloud backup:
    /// the content database file and its <c>-wal</c>/<c>-shm</c> sidecars.
    /// </summary>
    /// <param name="contentDatabasePath">The effective content database path.</param>
    /// <returns>The paths to exclude from the backup.</returns>
    public static IReadOnlyList<string> ExcludedPaths(ContentDatabasePath contentDatabasePath)
    {
        return WithSidecars(contentDatabasePath.FilePath);
    }

    /// <summary>
    /// Gets the paths that must be included in the cloud backup:
    /// the user database file and its <c>-wal</c>/<c>-shm</c> sidecars.
    /// Including a file that was never excluded is a harmless no-op; on
    /// upgraded installations it removes the exclusion flag set by earlier
    /// app versions.
    /// </summary>
    /// <param name="databasePath">The effective user database path.</param>
    /// <returns>The paths to include in the backup.</returns>
    public static IReadOnlyList<string> IncludedPaths(DatabasePath databasePath)
    {
        return WithSidecars(databasePath.FilePath);
    }

    private static IReadOnlyList<string> WithSidecars(string filePath)
    {
        return [filePath, filePath + "-wal", filePath + "-shm"];
    }
}
