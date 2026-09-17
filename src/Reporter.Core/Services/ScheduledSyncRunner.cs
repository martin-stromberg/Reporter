// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Executes the feed synchronization for an OS-scheduled background refresh task
/// and reschedules the next task via <see cref="IBackgroundRefreshService"/>.
/// </summary>
public class ScheduledSyncRunner : IScheduledSyncRunner
{
    private readonly IFeedSyncService _feedSyncService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IBackgroundRefreshService _backgroundRefreshService;
    private readonly IDebugLogService? _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledSyncRunner"/> class.
    /// </summary>
    /// <param name="feedSyncService">The feed sync service.</param>
    /// <param name="settingsRepository">The settings repository used to reschedule the next task.</param>
    /// <param name="backgroundRefreshService">The OS background refresh gateway used to reschedule the next task.</param>
    /// <param name="debugLogService">The optional session debug log service used to record failures.</param>
    public ScheduledSyncRunner(IFeedSyncService feedSyncService, ISettingsRepository settingsRepository, IBackgroundRefreshService backgroundRefreshService, IDebugLogService? debugLogService = null)
    {
        _feedSyncService = feedSyncService;
        _settingsRepository = settingsRepository;
        _backgroundRefreshService = backgroundRefreshService;
        _debugLogService = debugLogService;
    }

    /// <inheritdoc />
    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh sync started", level: DebugLogLevel.Info);
        var success = false;
        try
        {
            var result = await _feedSyncService.SyncAllAsync(cancellationToken).ConfigureAwait(false);
            success = result.Status != FeedHealth.Error;
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh sync finished", $"Status: {result.Status}, NewItems: {result.NewItems}", DebugLogLevel.Info);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ScheduledSyncRunner sync failed: {ex}");
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh sync failed", ex.ToString(), DebugLogLevel.Error);
        }

        // Der Folgeabruf wird auch im Fehler- und Kancellierungsfall neu
        // eingeplant, damit sich der Hintergrundabruf nicht totlaeuft.
        try
        {
            var settings = await _settingsRepository.GetAsync(CancellationToken.None).ConfigureAwait(false);
            await _backgroundRefreshService.ApplySettingsAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ScheduledSyncRunner rescheduling failed: {ex}");
            _ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Background refresh rescheduling failed", ex.ToString(), DebugLogLevel.Warning);
        }

        return success;
    }
}
