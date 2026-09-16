// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
#if IOS
using BackgroundTasks;
using CoreFoundation;
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

#if IOS
    // Submit darf erst laufen, nachdem die in AppDelegate verzoegert ausgefuehrte
    // BGTaskScheduler-Registrierung abgeschlossen ist — sonst wirft iOS eine
    // NSInternalInconsistencyException ("No launch handler registered").
    private static readonly TaskCompletionSource<bool> RegistrationCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

    private readonly IDebugLogService? _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundRefreshService"/> class.
    /// </summary>
    /// <param name="debugLogService">The optional session debug log service used to record failures.</param>
    public BackgroundRefreshService(IDebugLogService? debugLogService = null)
    {
        _debugLogService = debugLogService;
    }

#if IOS
    /// <summary>
    /// Meldet den Abschluss der <c>BGTaskScheduler</c>-Registrierung aus <see cref="AppDelegate"/>.
    /// </summary>
    /// <param name="registered"><c>true</c>, wenn <c>BGTaskScheduler.Register</c> erfolgreich war.</param>
    internal static void NotifyTaskRegistered(bool registered) =>
        RegistrationCompletion.TrySetResult(registered);
#endif

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
#if IOS
        return ApplySettingsIosAsync(settings, cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
#endif
    }

#if IOS
    private async Task ApplySettingsIosAsync(Settings settings, CancellationToken cancellationToken)
    {
        // Die Registrierung ist verzoegert (siehe AppDelegate.FinishedLaunching);
        // ein Start-Aufruf trifft sonst vor ihr ein. Beim Timeout wird nichts
        // eingeplant — ohne Registrierung waere Submit ohnehin wirkungslos.
        var registered = await WaitForRegistrationAsync(cancellationToken).ConfigureAwait(false);
        if (!registered)
        {
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh scheduling unavailable", "Task registration not completed", DebugLogLevel.Warning);
            return;
        }

        // Die Main-Queue serialisiert die Scheduler-Zugriffe hinter der
        // Registrierung und haelt alle BGTaskScheduler-Aufrufe auf einem Thread.
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                ApplyOnMainQueue(settings);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        await tcs.Task.ConfigureAwait(false);
    }

    private void ApplyOnMainQueue(Settings settings)
    {
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
                return;
            }

            var intervalMinutes = SettingsValues.ClampRefreshIntervalMinutes(settings.RefreshIntervalMinutes);
            var request = new BGAppRefreshTaskRequest(RefreshTaskIdentifier)
            {
                // iOS behandelt EarliestBeginDate nur als Untergrenze — der
                // tatsaechliche Ausfuehrungszeitpunkt liegt beim System.
                EarliestBeginDate = NSDate.FromTimeIntervalSinceNow(TimeSpan.FromMinutes(intervalMinutes).TotalSeconds),
            };

            try
            {
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
            catch (Exception ex)
            {
                // Objective-C-Exceptions (z. B. NSInternalInconsistencyException)
                // werden als ObjCException gemarshallt und sind hier abfaengbar.
                Debug.WriteLine($"BackgroundRefreshService submit threw: {ex}");
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh scheduling failed", ex.ToString(), DebugLogLevel.Warning);
            }
        }
        else
        {
            try
            {
                BGTaskScheduler.Shared.Cancel(RefreshTaskIdentifier);
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh unscheduled", $"Identifier: {RefreshTaskIdentifier}", DebugLogLevel.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackgroundRefreshService cancel threw: {ex}");
                _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh unscheduling failed", ex.ToString(), DebugLogLevel.Warning);
            }
        }
    }

    private static async Task<bool> WaitForRegistrationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await RegistrationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            Debug.WriteLine($"BackgroundRefreshService registration wait timed out: {ex}");
            return false;
        }
    }
#endif
}
