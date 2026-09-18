// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="BackupExclusionPlan"/> class.
/// </summary>
public class BackupExclusionPlanTests
{
    /// <summary>
    /// Verifies that the exclusion set covers the content database file and
    /// its SQLite <c>-wal</c>/<c>-shm</c> sidecars.
    /// </summary>
    [Fact]
    public void ExcludedPaths_ContainsContentDatabaseAndSidecars()
    {
        var contentDatabasePath = new ContentDatabasePath(Path.Combine("data", "reporter-content.db"));

        var paths = BackupExclusionPlan.ExcludedPaths(contentDatabasePath);

        Assert.Equal(
            new[]
            {
                Path.Combine("data", "reporter-content.db"),
                Path.Combine("data", "reporter-content.db") + "-wal",
                Path.Combine("data", "reporter-content.db") + "-shm",
            },
            paths);
    }

    /// <summary>
    /// Verifies that the re-include set covers the user database file and its
    /// SQLite <c>-wal</c>/<c>-shm</c> sidecars so the exclusion flag set by
    /// earlier app versions is removed.
    /// </summary>
    [Fact]
    public void IncludedPaths_ContainsUserDatabaseAndSidecars()
    {
        var databasePath = new DatabasePath(Path.Combine("data", "reporter.db"));

        var paths = BackupExclusionPlan.IncludedPaths(databasePath);

        Assert.Equal(
            new[]
            {
                Path.Combine("data", "reporter.db"),
                Path.Combine("data", "reporter.db") + "-wal",
                Path.Combine("data", "reporter.db") + "-shm",
            },
            paths);
    }
}
