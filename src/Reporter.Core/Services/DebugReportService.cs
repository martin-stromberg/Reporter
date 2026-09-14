// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using System.Text;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Implements <see cref="IDebugReportService"/>: collects the debug report data
/// (application and device information, network status, settings snapshot, feed
/// health, sync history and the session debug log), formats a plain-text body and
/// hands it to the system mail client via <see cref="IEmailService"/>.
/// </summary>
public class DebugReportService : IDebugReportService
{
    /// <summary>
    /// The recipient address of the debug report e-mail.
    /// </summary>
    /// <remarks>
    /// PLACEHOLDER — <c>debug@example.com</c> is not a real mailbox. The maintainer
    /// must replace it with the actual support address before release (product
    /// decision: fixed constant, not user-configurable).
    /// </remarks>
    public const string DebugReportRecipient = "debug@example.com";

    /// <summary>
    /// The maximum number of sync log entries included in the report body.
    /// </summary>
    public const int MaxSyncLogEntries = 50;

    /// <summary>
    /// The maximum number of session debug log entries included in the report body.
    /// </summary>
    public const int MaxDebugLogEntries = 200;

    private readonly ISettingsRepository _settingsRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly IDebugLogRepository _debugLogRepository;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly INetworkStatusService _networkStatusService;
    private readonly IEmailService _emailService;
    private readonly IDebugLogService? _debugLogService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugReportService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="syncLogRepository">The sync log repository.</param>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="debugLogRepository">The session debug log repository.</param>
    /// <param name="deviceInfoProvider">The application and device information provider.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    /// <param name="emailService">The e-mail compose gateway.</param>
    /// <param name="timeProvider">The time provider used for the report timestamp.</param>
    /// <param name="debugLogService">The optional session debug log service used to record the report action.</param>
    public DebugReportService(
        ISettingsRepository settingsRepository,
        ISyncLogRepository syncLogRepository,
        IFeedRepository feedRepository,
        IDebugLogRepository debugLogRepository,
        IDeviceInfoProvider deviceInfoProvider,
        INetworkStatusService networkStatusService,
        IEmailService emailService,
        TimeProvider? timeProvider = null,
        IDebugLogService? debugLogService = null)
    {
        _settingsRepository = settingsRepository;
        _syncLogRepository = syncLogRepository;
        _feedRepository = feedRepository;
        _debugLogRepository = debugLogRepository;
        _deviceInfoProvider = deviceInfoProvider;
        _networkStatusService = networkStatusService;
        _emailService = emailService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _debugLogService = debugLogService;
    }

    /// <inheritdoc />
    public bool IsSupported => _emailService.IsSupported;

    /// <inheritdoc />
    public async Task<bool> SendReportAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            await LogReportEntryAsync(
                "Debug report not sent: e-mail compose is not supported on this device",
                DebugLogLevel.Error,
                cancellationToken).ConfigureAwait(false);
            return false;
        }

        var deviceInfo = _deviceInfoProvider.GetSnapshot();
        var isOnline = _networkStatusService.IsOnline;
        var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
        var feeds = await _feedRepository.GetAllAsync().ConfigureAwait(false);
        var syncLogs = await _syncLogRepository.GetLatestAsync(MaxSyncLogEntries).ConfigureAwait(false);
        var debugLogEntries = await _debugLogRepository.GetLatestAsync(MaxDebugLogEntries).ConfigureAwait(false);

        var subject = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.DebugReportEmailSubject,
            $"{deviceInfo.AppName} {deviceInfo.AppVersion}");
        var body = BuildBody(deviceInfo, isOnline, settings, feeds, syncLogs, debugLogEntries);

        var sent = await _emailService.ComposeAsync(DebugReportRecipient, subject, body, cancellationToken).ConfigureAwait(false);
        await LogReportEntryAsync(
            sent ? "Debug report composed" : "Debug report compose failed",
            sent ? DebugLogLevel.Info : DebugLogLevel.Error,
            cancellationToken).ConfigureAwait(false);
        return sent;
    }

    private string BuildBody(
        AppDeviceInfo deviceInfo,
        bool isOnline,
        Settings settings,
        IReadOnlyList<Feed> feeds,
        IEnumerable<SyncLog> syncLogs,
        IEnumerable<DebugLogEntry> debugLogEntries)
    {
        var builder = new StringBuilder();
        var generatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionAppInfo} ==");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Name: {deviceInfo.AppName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Version: {deviceInfo.AppVersion}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Build: {deviceInfo.AppBuild}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Report generated at: {generatedAt:u}");
        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionDevice} ==");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Model: {deviceInfo.DeviceModel}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Manufacturer: {deviceInfo.DeviceManufacturer}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Platform: {deviceInfo.Platform}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"OS: {deviceInfo.OsVersion}");
        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionNetwork} ==");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Online: {isOnline}");
        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionSettings} ==");
        builder.AppendLine(CultureInfo.InvariantCulture, $"RetentionDays: {settings.RetentionDays}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AutoMarkReadMode: {settings.AutoMarkReadMode}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AutoMarkReadDelaySeconds: {settings.AutoMarkReadDelaySeconds}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"NotificationsEnabled: {settings.NotificationsEnabled}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"NotificationSummaryEnabled: {settings.NotificationSummaryEnabled}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"QuietHoursStart: {FormatNullable(settings.QuietHoursStart)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"QuietHoursEnd: {FormatNullable(settings.QuietHoursEnd)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AutoRefreshEnabled: {settings.AutoRefreshEnabled}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"RefreshIntervalMinutes: {settings.RefreshIntervalMinutes}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"RefreshOnStartupEnabled: {settings.RefreshOnStartupEnabled}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"UnreadSortOrder: {settings.UnreadSortOrder}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Theme: {settings.Theme}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Language: {settings.Language}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"DebugCollectionEnabled: {settings.DebugCollectionEnabled}");
        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionFeedHealth} ==");
        foreach (var feed in feeds)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {feed.Title} | {feed.Url} | Status: {feed.HealthStatus} | LastCheckedAt: {FormatNullable(feed.LastCheckedAt)} | HealthLastChange: {FormatNullable(feed.HealthLastChange)}");
        }

        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionSyncLog} ==");
        foreach (var log in syncLogs)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"[{FormatNullable(log.StartedAt)}] {log.Status}: {log.Message}");
        }

        builder.AppendLine();

        builder.AppendLine(CultureInfo.InvariantCulture, $"== {AppResources.DebugReportSectionSessionLog} ==");
        foreach (var entry in debugLogEntries)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"[{entry.Timestamp:u}] {entry.Level}/{entry.Category}: {entry.Message}");
            if (!string.IsNullOrEmpty(entry.Details))
            {
                builder.AppendLine(entry.Details);
            }
        }

        return builder.ToString();
    }

    private async Task LogReportEntryAsync(string message, string level, CancellationToken cancellationToken)
    {
        if (_debugLogService is not null)
        {
            await _debugLogService.LogAsync(
                DebugLogCategory.Report,
                message,
                level: level,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private static string FormatNullable(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("u", CultureInfo.InvariantCulture)
            : "-";
    }

    private static string FormatNullable(TimeSpan? value)
    {
        return value?.ToString("c", CultureInfo.InvariantCulture) ?? "-";
    }
}
