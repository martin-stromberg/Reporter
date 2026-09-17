// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Platform gateway that excludes a file from the OS-managed cloud backup
/// (iCloud backup on iOS). On platforms without such a backup concept the
/// implementation is a no-op.
/// </summary>
public interface IBackupExclusionService
{
    /// <summary>
    /// Marks the file at <paramref name="filePath"/> as excluded from the
    /// cloud backup. Missing files are tolerated (no-op); implementations do
    /// not throw for absent paths.
    /// </summary>
    /// <param name="filePath">The path of the file to exclude.</param>
    void ExcludeFromBackup(string filePath);
}
