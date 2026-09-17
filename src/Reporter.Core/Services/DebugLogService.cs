// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Implements <see cref="IDebugLogService"/>: records session debug log entries
/// while collection is enabled and resets the log on every session start,
/// keeping <see cref="DebugLogLevel.Error"/> entries (crash reports) of the
/// previous session so they remain sendable after a restart.
/// The logger sits inside error paths and therefore never throws — internal
/// failures are swallowed to <see cref="Debug"/>.
/// </summary>
public class DebugLogService : IDebugLogService
{
    /// <summary>
    /// The maximum number of debug log entries kept in the session log table.
    /// </summary>
    public const int MaxStoredEntries = 500;

    private readonly IDebugLogRepository _debugLogRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly TimeProvider _timeProvider;
    private volatile bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogService"/> class.
    /// </summary>
    /// <param name="debugLogRepository">The debug log repository.</param>
    /// <param name="settingsRepository">The settings repository used to load the persisted collection switch.</param>
    /// <param name="timeProvider">The time provider used for entry timestamps.</param>
    public DebugLogService(IDebugLogRepository debugLogRepository, ISettingsRepository settingsRepository, TimeProvider? timeProvider = null)
    {
        _debugLogRepository = debugLogRepository;
        _settingsRepository = settingsRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
    public async Task BeginSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Die Vorsession wird zurückgesetzt, aber Error-Einträge (z. B. Absturzberichte)
            // bleiben erhalten, damit sie nach einem Neustart noch versendet werden können.
            await _debugLogRepository.DeleteAllExceptErrorsAsync().ConfigureAwait(false);
            var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
            _enabled = settings.DebugCollectionEnabled;

            if (_enabled)
            {
                await LogAsync(DebugLogCategory.Lifecycle, "Debug session started", level: DebugLogLevel.Info, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Das Logging darf den App-Start niemals verhindern.
            Debug.WriteLine($"DebugLogService.BeginSessionAsync failed: {ex}");
        }
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        var wasEnabled = _enabled;
        _enabled = enabled;

        if (enabled && !wasEnabled)
        {
            _ = LogAsync(DebugLogCategory.Lifecycle, "Debug collection enabled", level: DebugLogLevel.Info);
        }
    }

    /// <inheritdoc />
    public async Task LogAsync(string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken cancellationToken = default)
    {
        if (!_enabled || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var entry = new DebugLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                Level = level,
                Category = category,
                Message = message,
                Details = details,
            };
            await _debugLogRepository.AddAsync(entry).ConfigureAwait(false);
            await _debugLogRepository.TrimToLatestAsync(MaxStoredEntries).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Der Logger sitzt in Fehlerpfaden und darf selbst keine Ausnahmequelle sein.
            Debug.WriteLine($"DebugLogService.LogAsync failed: {ex}");
        }
    }
}
