// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using BackgroundTasks;
using CoreFoundation;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Core.Services;
using Reporter.Services;
using UIKit;
using UserNotifications;

namespace Reporter;

/// <summary>
/// The iOS application delegate for the .NET MAUI application.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    // Die native Delegate-Property von UNUserNotificationCenter ist weak — die Instanz
    // muss verwaltet gehalten werden, sonst kann der GC sie freigeben.
    private readonly NotificationDelegate _notificationDelegate = new();

    /// <summary>
    /// Creates the <see cref="MauiApp"/> for this platform.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <inheritdoc />
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        UNUserNotificationCenter.Current.Delegate = _notificationDelegate;
        var result = base.FinishedLaunching(application, launchOptions);
        // Die Registrierung wird auf den naechsten Main-Queue-Durchlauf verlagert:
        // Ein synchroner Aufruf waehrend FinishedLaunching kollidiert mit dem
        // internen Launch-Handling von BGTaskScheduler (_os_unfair_lock_recursive_abort).
        DispatchQueue.MainQueue.DispatchAsync(RegisterBackgroundFetchTask);
        return result;
    }

    private void RegisterBackgroundFetchTask()
    {
        var registered = BGTaskScheduler.Shared.Register(BackgroundRefreshService.RefreshTaskIdentifier, null, task =>
        {
            if (task is BGAppRefreshTask refreshTask)
            {
                _ = HandleRefreshTaskAsync(refreshTask);
            }
        });

        var debugLogService = IPlatformApplication.Current?.Services?.GetService<IDebugLogService>();
        if (!registered)
        {
            Debug.WriteLine($"AppDelegate background task registration failed: {BackgroundRefreshService.RefreshTaskIdentifier}");
            _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Background task registration failed", $"Identifier: {BackgroundRefreshService.RefreshTaskIdentifier}", DebugLogLevel.Warning);
        }
        else
        {
            _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Background task registered", $"Identifier: {BackgroundRefreshService.RefreshTaskIdentifier}", DebugLogLevel.Info);
        }
    }

    private async Task HandleRefreshTaskAsync(BGAppRefreshTask task)
    {
        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource();
        task.ExpirationHandler = cts.Cancel;

        var success = false;
        IDebugLogService? debugLogService = null;
        try
        {
            var services = IPlatformApplication.Current?.Services;
            debugLogService = services?.GetService<IDebugLogService>();
            _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh task started", $"Identifier: {task.Identifier}", DebugLogLevel.Info);
            var scheduledSyncRunner = services?.GetService<IScheduledSyncRunner>();
            if (scheduledSyncRunner is not null)
            {
                success = await scheduledSyncRunner.RunAsync(cts.Token).ConfigureAwait(false);
            }
            else
            {
                Debug.WriteLine("AppDelegate scheduled sync runner not resolved");
                _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Scheduled sync runner not resolved", null, DebugLogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            // Ein Fehler im Hintergrund-Task darf den App-Lebenszyklus nicht beeintraechtigen.
            Debug.WriteLine($"AppDelegate refresh task failed: {ex}");
            _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh task failed", ex.ToString(), DebugLogLevel.Warning);
            success = false;
        }
        finally
        {
            _ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh task completed", $"Success: {success}, elapsed: {stopwatch.ElapsedMilliseconds} ms", DebugLogLevel.Info);
            task.SetTaskCompleted(success);
        }
    }
}
