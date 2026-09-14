// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="DebugReportService"/> class.
/// </summary>
public class DebugReportServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly FeedRepository _feedRepository;
    private readonly DebugLogRepository _debugLogRepository;
    private readonly FakeDeviceInfoProvider _deviceInfoProvider;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly FakeEmailService _emailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugReportServiceTests"/> class.
    /// </summary>
    public DebugReportServiceTests()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _feedRepository = new FeedRepository(_factory);
        _debugLogRepository = new DebugLogRepository(_factory);
        _deviceInfoProvider = new FakeDeviceInfoProvider();
        _networkStatusService = new FakeNetworkStatusService();
        _emailService = new FakeEmailService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private DebugReportService CreateService(FakeDebugLogService? debugLogService = null, FakeTimeProvider? timeProvider = null)
    {
        return new DebugReportService(
            _settingsRepository,
            _syncLogRepository,
            _feedRepository,
            _debugLogRepository,
            _deviceInfoProvider,
            _networkStatusService,
            _emailService,
            timeProvider,
            debugLogService);
    }

    /// <summary>
    /// Verifies that SendReportAsync returns false without composing when e-mail is unsupported.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_Unsupported_ReturnsFalse_AndDoesNotCompose()
    {
        _emailService.IsSupported = false;
        var service = CreateService();

        var result = await service.SendReportAsync();

        Assert.False(result);
        Assert.Empty(_emailService.ComposedEmails);
    }

    /// <summary>
    /// Verifies that SendReportAsync composes an e-mail with the debug recipient and the localized subject.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_ComposesWithRecipientAndLocalizedSubject()
    {
        var service = CreateService();

        var result = await service.SendReportAsync();

        Assert.True(result);
        var email = Assert.Single(_emailService.ComposedEmails);
        Assert.Equal(DebugReportService.DebugReportRecipient, email.Recipient);
        var expectedSubject = string.Format(CultureInfo.CurrentCulture, AppResources.DebugReportEmailSubject, "Reporter 1.2.3");
        Assert.Equal(expectedSubject, email.Subject);
    }

    /// <summary>
    /// Verifies that the report body contains the application, device and network information.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_BodyContainsDeviceAppAndNetworkInfo()
    {
        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains("Reporter", body);
        Assert.Contains("1.2.3", body);
        Assert.Contains("42", body);
        Assert.Contains("Pixel 8", body);
        Assert.Contains("Google", body);
        Assert.Contains("Android", body);
        Assert.Contains("34", body);
        Assert.Contains("Online: True", body);
        Assert.Contains(AppResources.DebugReportSectionAppInfo, body);
        Assert.Contains(AppResources.DebugReportSectionDevice, body);
        Assert.Contains(AppResources.DebugReportSectionNetwork, body);
    }

    /// <summary>
    /// Verifies that the report body contains the settings snapshot including the debug collection switch.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_BodyContainsSettings()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true, language: "de");
        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains(AppResources.DebugReportSectionSettings, body);
        Assert.Contains("DebugCollectionEnabled: True", body);
        Assert.Contains("Language: de", body);
    }

    /// <summary>
    /// Verifies that the report body contains the feed health information.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_BodyContainsFeedHealth()
    {
        var lastChecked = new DateTime(2026, 9, 14, 6, 0, 0, DateTimeKind.Utc);
        await _feedRepository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/broken",
            Title = "Broken Feed",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Error,
            LastCheckedAt = lastChecked,
            HealthLastChange = lastChecked,
        });
        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains(AppResources.DebugReportSectionFeedHealth, body);
        Assert.Contains("Broken Feed", body);
        Assert.Contains("https://example.com/broken", body);
        Assert.Contains(FeedHealth.Error, body);
    }

    /// <summary>
    /// Verifies that the report body contains the newest sync log entries in descending order.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_BodyContainsLatestSyncLogs()
    {
        var now = DateTime.UtcNow;
        await _syncLogRepository.AddAsync(new SyncLog { Id = Guid.NewGuid(), StartedAt = now.AddMinutes(-10), Status = FeedHealth.Ok, Message = "older sync" });
        await _syncLogRepository.AddAsync(new SyncLog { Id = Guid.NewGuid(), StartedAt = now, Status = FeedHealth.Error, Message = "newer sync failed" });
        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains(AppResources.DebugReportSectionSyncLog, body);
        Assert.Contains("newer sync failed", body);
        Assert.Contains("older sync", body);
        Assert.True(
            body.IndexOf("newer sync failed", StringComparison.Ordinal) < body.IndexOf("older sync", StringComparison.Ordinal),
            "expected newest sync log first");
    }

    /// <summary>
    /// Verifies that the report body limits the sync log to the newest 50 entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_LimitsSyncLogsTo50()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 60; i++)
        {
            await _syncLogRepository.AddAsync(new SyncLog { Id = Guid.NewGuid(), StartedAt = now.AddSeconds(i), Status = FeedHealth.Ok, Message = $"sync-entry-{i:D2}" });
        }

        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains("sync-entry-59", body);
        Assert.Contains("sync-entry-10", body);
        Assert.DoesNotContain("sync-entry-09", body);
    }

    /// <summary>
    /// Verifies that the report body contains the session debug log entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_BodyContainsSessionDebugLog()
    {
        await _debugLogRepository.AddAsync(new DebugLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Level = DebugLogLevel.Error,
            Category = DebugLogCategory.Exception,
            Message = "session exploded",
            Details = "stacktrace here",
        });
        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains(AppResources.DebugReportSectionSessionLog, body);
        Assert.Contains("session exploded", body);
        Assert.Contains("stacktrace here", body);
    }

    /// <summary>
    /// Verifies that the report body limits the session debug log to the newest 200 entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_LimitsDebugLogsTo200()
    {
        var now = DateTime.UtcNow;
        await using (var context = _factory.CreateDbContext())
        {
            var entities = Enumerable
                .Range(0, 210)
                .Select(i => new Reporter.Data.Entities.DebugLogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = now.AddSeconds(i),
                    Level = DebugLogLevel.Info,
                    Category = DebugLogCategory.Lifecycle,
                    Message = $"debug-entry-{i:D3}",
                });
            context.DebugLogEntries.AddRange(entities);
            await context.SaveChangesAsync();
        }

        var service = CreateService();

        await service.SendReportAsync();

        var body = Assert.Single(_emailService.ComposedEmails).Body;
        Assert.Contains("debug-entry-209", body);
        Assert.Contains("debug-entry-010", body);
        Assert.DoesNotContain("debug-entry-009", body);
    }

    /// <summary>
    /// Verifies that an empty session log and empty sync history still produce a report.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_EmptyLogs_StillComposes()
    {
        var service = CreateService();

        var result = await service.SendReportAsync();

        Assert.True(result);
        Assert.Single(_emailService.ComposedEmails);
    }

    /// <summary>
    /// Verifies that SendReportAsync returns false when the mail client reports compose as not sent.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_ComposeReturnsFalse_ReturnsFalse()
    {
        _emailService.ComposeResult = false;
        var service = CreateService();

        var result = await service.SendReportAsync();

        Assert.False(result);
        Assert.Single(_emailService.ComposedEmails);
    }

    /// <summary>
    /// Verifies that SendReportAsync records a report entry in the session debug log service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_LogsReportEntry()
    {
        var debugLogService = new FakeDebugLogService { IsEnabled = true };
        var service = CreateService(debugLogService: debugLogService);

        await service.SendReportAsync();

        var entry = Assert.Single(debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Report, entry.Category);
        Assert.Equal(DebugLogLevel.Info, entry.Level);
    }

    /// <summary>
    /// Verifies that SendReportAsync records an error entry in the session debug log when composing fails.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_ComposeReturnsFalse_LogsErrorEntry()
    {
        _emailService.ComposeResult = false;
        var debugLogService = new FakeDebugLogService { IsEnabled = true };
        var service = CreateService(debugLogService: debugLogService);

        var result = await service.SendReportAsync();

        Assert.False(result);
        var entry = Assert.Single(debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Report, entry.Category);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
    }

    /// <summary>
    /// Verifies that the report recipient is read from the DebugReportRecipient
    /// MSBuild property embedded as assembly metadata.
    /// </summary>
    [Fact]
    public void DebugReportRecipient_ReadsAssemblyMetadata()
    {
        Assert.Equal("debug@example.com", DebugReportService.DebugReportRecipient);
    }

    /// <summary>
    /// Verifies that SendReportAsync records an error entry in the session debug log when e-mail is unsupported.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendReportAsync_Unsupported_LogsErrorEntry()
    {
        _emailService.IsSupported = false;
        var debugLogService = new FakeDebugLogService { IsEnabled = true };
        var service = CreateService(debugLogService: debugLogService);

        var result = await service.SendReportAsync();

        Assert.False(result);
        var entry = Assert.Single(debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Report, entry.Category);
        Assert.Equal(DebugLogLevel.Error, entry.Level);
    }
}
