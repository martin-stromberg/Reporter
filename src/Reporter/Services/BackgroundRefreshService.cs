// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
#if IOS
using BackgroundTasks;
using Foundation;
using UIKit;
#endif

namespace Reporter.Services;

/// <summary>
/// Schedules the iOS <c>BGAppRefreshTask</c> via <c>BGTaskScheduler</c>. The feed
/// sync for a launched task is executed by <see cref="ScheduledSyncRunner"/>.
/// On other platforms the service is a no-op.
/// </summary>
public class BackgroundRefreshService : IBackgroundRefreshService
{
    /// <summary>
    /// The background refresh task identifier. Must match an entry of
    /// <c>BGTaskSchedulerPermittedIdentifiers</c> in the iOS <c>Info.plist</c>.
    /// </summary>
    public const string RefreshTaskIdentifier = "de.martinstromberg.reporter.feedrefresh";

    private readonly IDebugLogService? _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundRefreshService"/> class.
    /// </summary>
    /// <param name="debugLogService">The optional session debug log service used to record failures.</param>
    public BackgroundRefreshService(IDebugLogService? debugLogService = null)
    {
        _debugLogService = debugLogService;
    }

    /// <inheritdoc />
    public bool IsSupported =>
#if IOS
        true;
#else
        false;
#endif

    /// <inheritdoc />
    public Task ApplySettingsAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if IOS
        if (settings.AutoRefreshEnabled)
        {
            var refreshStatus = UIApplication.SharedApplication.BackgroundRefreshStatus;
            if (refreshStatus != UIBackgroundRefreshStatus.Available)
            {
                // Deaktiviert in den iOS-Einstellungen oder eingeschraenkt (z. B.
                // Energiesparmodus, Bildschirmzeit) — Submit wuerde mit
                // BGTaskSchedulerErrorCode.Unavailable fehlschlagen.
                Debug.WriteLine($"BackgroundRefreshService scheduling unavailable: {refreshStatus}");
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh scheduling unavailable", $"BackgroundRefreshStatus: {refreshStatus}", DebugLogLevel.Warning);
                return Task.CompletedTask;
            }

            var intervalMinutes = SettingsValues.ClampRefreshIntervalMinutes(settings.RefreshIntervalMinutes);
            var request = new BGAppRefreshTaskRequest(RefreshTaskIdentifier)
            {
                // iOS behandelt EarliestBeginDate nur als Untergrenze — der
                // tatsaechliche Ausfuehrungszeitpunkt liegt beim System.
                EarliestBeginDate = NSDate.FromTimeIntervalSinceNow(TimeSpan.FromMinutes(intervalMinutes).TotalSeconds),
            };

            if (!BGTaskScheduler.Shared.Submit(request, out var error))
            {
                Debug.WriteLine($"BackgroundRefreshService submit failed: {error?.LocalizedDescription}");
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh scheduling failed", error?.ToString(), DebugLogLevel.Warning);
            }
            else
            {
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh scheduled", $"Identifier: {RefreshTaskIdentifier}, earliest: {request.EarliestBeginDate}, interval: {intervalMinutes} min", DebugLogLevel.Info);
            }
        }
        else
        {
            BGTaskScheduler.Shared.Cancel(RefreshTaskIdentifier);
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh unscheduled", $"Identifier: {RefreshTaskIdentifier}", DebugLogLevel.Info);
        }
#endif
        return Task.CompletedTask;
    }
}
