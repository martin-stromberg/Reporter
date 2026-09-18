// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Carries the effective database file path resolved in <c>MauiProgram</c>
/// (including the <c>REPORTER_DB_PATH</c> override) into the service container,
/// so platform services can address the database file without duplicating
/// the path resolution.
/// </summary>
public class DatabasePath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePath"/> class.
    /// </summary>
    /// <param name="filePath">The effective database file path.</param>
    public DatabasePath(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Gets the effective database file path.
    /// </summary>
    public string FilePath { get; }
}
