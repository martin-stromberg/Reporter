// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
#if IOS
using System.Diagnostics;
using Foundation;
#endif

namespace Reporter.Services;

/// <summary>
/// Excludes files from the iCloud backup on iOS by setting
/// <c>NSUrl.IsExcludedFromBackupKey</c>. On other platforms the service is a
/// no-op. The local SQLite database holds all user data locally and must not
/// count against the user's iCloud backup quota (App Store review guideline
/// 2.23 / iOS Data Storage Guidelines).
/// </summary>
public class BackupExclusionService : IBackupExclusionService
{
    /// <inheritdoc />
    public void ExcludeFromBackup(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

#if IOS
        using var url = NSUrl.FromFilename(filePath);
        using var value = NSNumber.FromBoolean(true);
        if (!url.SetResource(NSUrl.IsExcludedFromBackupKey, value, out var error))
        {
            Debug.WriteLine($"BackupExclusionService could not exclude '{filePath}' from backup: {error?.LocalizedDescription}");
        }
#endif
    }
}
