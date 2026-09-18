// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
#if IOS
using System.Diagnostics;
using Foundation;
#endif

namespace Reporter.Services;

/// <summary>
/// Sets the iCloud backup flag on iOS via <c>NSUrl.IsExcludedFromBackupKey</c>:
/// <see cref="ExcludeFromBackup"/> marks a file as excluded,
/// <see cref="IncludeInBackup"/> removes a previously set exclusion. On other
/// platforms the service is a no-op. The content database holds only
/// re-downloadable article contents and stays excluded, while the user
/// database is included so subscriptions, settings and reading state survive
/// a device restore (iOS Data Storage Guidelines).
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

    /// <inheritdoc />
    public void IncludeInBackup(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

#if IOS
        using var url = NSUrl.FromFilename(filePath);
        using var value = NSNumber.FromBoolean(false);
        if (!url.SetResource(NSUrl.IsExcludedFromBackupKey, value, out var error))
        {
            Debug.WriteLine($"BackupExclusionService could not include '{filePath}' in backup: {error?.LocalizedDescription}");
        }
#endif
    }
}
