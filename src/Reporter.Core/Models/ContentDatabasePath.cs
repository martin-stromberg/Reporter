// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Carries the effective content database file path resolved in <c>MauiProgram</c>
/// (derived from the <c>reporter.db</c> directory including the
/// <c>REPORTER_DB_PATH</c> override) into the service container, so platform
/// services can address the content database file without duplicating the
/// path resolution.
/// </summary>
public class ContentDatabasePath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentDatabasePath"/> class.
    /// </summary>
    /// <param name="filePath">The effective content database file path.</param>
    public ContentDatabasePath(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Gets the effective content database file path.
    /// </summary>
    public string FilePath { get; }
}
