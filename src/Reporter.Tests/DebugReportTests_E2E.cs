// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains end-to-end tests for the debug collection and debug report flow:
/// settings persistence, session log lifecycle and report composition through the
/// real services backed by SQLite repositories.
/// </summary>
public class DebugReportTests_E2E : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly FeedRepository _feedRepository;
    private readonly DebugLogRepository _debugLogRepository;
    private readonly FakeEmailService _emailService;
    private readonly FakeDeviceInfoProvider _deviceInfoProvider;
    private readonly FakeNetworkStatusService _networkStatusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugReportTests_E2E"/> class.
    /// </summary>
    public DebugReportTests_E2E()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _feedRepository = new FeedRepository(_factory);
        _debugLogRepository = new DebugLogRepository(_factory);
        _emailService = new FakeEmailService();
        _deviceInfoProvider = new FakeDeviceInfoProvider();
        _networkStatusService = new FakeNetworkStatusService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private DebugReportService CreateReportService(IDebugLogService? debugLogService)
    {
        return new DebugReportService(
            _settingsRepository,
            _syncLogRepository,
            _feedRepository,
            _debugLogRepository,
            _deviceInfoProvider,
            _networkStatusService,
            _emailService,
            debugLogService: debugLogService);
    }

    /// <summary>
    /// Verifies that the debug collection switch persists across a simulated app restart:
    /// a new session service loads the persisted value.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DebugCollection_PersistedAcrossSessions()
    {
        var firstSession = new DebugLogService(_debugLogRepository, _settingsRepository);
        var viewModel = new SettingsViewModel(
            _settingsRepository,
            _keywordRepository,
            new FakeAutoRefreshService(),
            new FakeAppThemeService(),
            debugReportService: CreateReportService(firstSession),
            debugLogService: firstSession);
        await firstSession.BeginSessionAsync();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.DebugCollectionEnabled = true;
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).DebugCollectionEnabled);
        await TestWaitHelper.WaitUntilAsync(async () => (await _debugLogRepository.GetAllAsync()).Count == 1);

        var secondSession = new DebugLogService(_debugLogRepository, _settingsRepository);
        await secondSession.BeginSessionAsync();

        Assert.True(secondSession.IsEnabled);
        var entries = await _debugLogRepository.GetAllAsync();
        var startEntry = Assert.Single(entries);
        Assert.Equal("Debug session started", startEntry.Message);
    }

    /// <summary>
    /// Verifies that a debug report is composed end-to-end through the real report
    /// service and the real SQLite repositories.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Report_ComposesThroughRealServicesAndSqlite()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true);
        var session = new DebugLogService(_debugLogRepository, _settingsRepository);
        await session.BeginSessionAsync();
        await session.LogAsync(DebugLogCategory.Sync, "e2e sync error", "timeout", DebugLogLevel.Error);
        await _feedRepository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/e2e",
            Title = "E2E Feed",
            NotificationsEnabled = true,
            HealthStatus = FeedHealth.Ok,
        });
        await _syncLogRepository.AddAsync(new SyncLog
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            Status = FeedHealth.Ok,
            Message = "e2e sync ok",
        });

        var reportService = CreateReportService(session);
        var result = await reportService.SendReportAsync();

        Assert.True(result);
        var email = Assert.Single(_emailService.ComposedEmails);
        Assert.Equal(DebugReportService.DebugReportRecipient, email.Recipient);
        Assert.Contains("E2E Feed", email.Body);
        Assert.Contains("e2e sync ok", email.Body);
        Assert.Contains("e2e sync error", email.Body);
        Assert.Contains("DebugCollectionEnabled: True", email.Body);
    }

    /// <summary>
    /// Verifies the full session log flow: entries are written while enabled and the
    /// log is reset on the next session start, except error entries — a crash entry
    /// of the previous session stays sendable after the restart.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SessionLog_WritesAndResetsAcrossSessions()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true);
        var firstSession = new DebugLogService(_debugLogRepository, _settingsRepository);
        await firstSession.BeginSessionAsync();
        await firstSession.LogAsync(DebugLogCategory.Exception, "crash", "details", DebugLogLevel.Error);
        await firstSession.LogAsync(DebugLogCategory.Lifecycle, "App resumed");

        Assert.Equal(3, (await _debugLogRepository.GetAllAsync()).Count);

        var secondSession = new DebugLogService(_debugLogRepository, _settingsRepository);
        await secondSession.BeginSessionAsync();

        var entries = await _debugLogRepository.GetAllAsync();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Message == "crash" && e.Level == DebugLogLevel.Error);
        Assert.Contains(entries, e => e.Message == "Debug session started");
        Assert.DoesNotContain(entries, e => e.Message == "App resumed");
    }
}
