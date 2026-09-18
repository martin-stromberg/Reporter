// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the debug collection and debug report parts of the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests_Debug : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly SyncLogRepository _syncLogRepository;
    private readonly FeedRepository _feedRepository;
    private readonly DebugLogRepository _debugLogRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeEmailService _emailService;
    private readonly FakeDeviceInfoProvider _deviceInfoProvider;
    private readonly FakeNetworkStatusService _networkStatusService;
    private readonly FakeDebugLogService _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_Debug"/> class.
    /// </summary>
    public SettingsViewModelTests_Debug()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _syncLogRepository = new SyncLogRepository(_factory);
        _feedRepository = new FeedRepository(_factory, new FakeItemContentStore());
        _debugLogRepository = new DebugLogRepository(_factory);
        _autoRefreshService = new FakeAutoRefreshService();
        _appThemeService = new FakeAppThemeService();
        _timeProvider = new FakeTimeProvider();
        _emailService = new FakeEmailService();
        _deviceInfoProvider = new FakeDeviceInfoProvider();
        _networkStatusService = new FakeNetworkStatusService();
        _debugLogService = new FakeDebugLogService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private SettingsViewModel CreateViewModel(bool includeDebugServices = true)
    {
        var debugReportService = new DebugReportService(
            _settingsRepository,
            _syncLogRepository,
            _feedRepository,
            _debugLogRepository,
            _deviceInfoProvider,
            _networkStatusService,
            _emailService,
            _timeProvider,
            _debugLogService);
        return new SettingsViewModel(
            _settingsRepository,
            _keywordRepository,
            _autoRefreshService,
            _appThemeService,
            _timeProvider,
            localNotificationService: null,
            debugReportService: includeDebugServices ? debugReportService : null,
            debugLogService: includeDebugServices ? _debugLogService : null);
    }

    /// <summary>
    /// Verifies that LoadAsync reads the persisted debug collection switch.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_ReadsPersistedDebugSwitch()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.DebugCollectionEnabled);
        Assert.True(viewModel.DebugSendEnabled);
    }

    /// <summary>
    /// Verifies that LoadAsync does not call SetEnabled on the session debug log
    /// while the persisted switch is applied (isLoading guard).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PersistedDebugSwitch_DoesNotCallSetEnabled()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, debugCollectionEnabled: true);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.DebugCollectionEnabled);
        Assert.Empty(_debugLogService.SetEnabledCalls);
    }

    /// <summary>
    /// Verifies that toggling the debug collection switch persists the value and
    /// switches the session debug log immediately.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DebugCollectionEnabled_Toggle_PersistsAndSwitchesLog()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        Assert.False(viewModel.DebugCollectionEnabled);

        viewModel.DebugCollectionEnabled = true;

        Assert.Single(_debugLogService.SetEnabledCalls);
        Assert.True(_debugLogService.SetEnabledCalls[0]);
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).DebugCollectionEnabled);
    }

    /// <summary>
    /// Verifies that the send action is only enabled when collection is on and e-mail is supported.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DebugSendEnabled_RequiresCollectionAndEmailSupport()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.DebugSendEnabled);

        viewModel.DebugCollectionEnabled = true;
        Assert.True(viewModel.DebugSendEnabled);

        _emailService.IsSupported = false;
        // DebugEmailSupported is computed live from the report service.
        Assert.False(viewModel.DebugEmailSupported);
        Assert.False(viewModel.DebugSendEnabled);
    }

    /// <summary>
    /// Verifies that SendDebugReportCommand composes an e-mail through the real report service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendDebugReport_Enabled_ComposesEmail()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.DebugCollectionEnabled = true;

        await viewModel.SendDebugReportCommand.ExecuteAsync(null);

        var email = Assert.Single(_emailService.ComposedEmails);
        Assert.Equal(DebugReportService.DebugReportRecipient, email.Recipient);
        Assert.Contains("Reporter", email.Body);
    }

    /// <summary>
    /// Verifies that SendDebugReportCommand does nothing while collection is disabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendDebugReport_Disabled_DoesNotCompose()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.SendDebugReportCommand.ExecuteAsync(null);

        Assert.Empty(_emailService.ComposedEmails);
    }

    /// <summary>
    /// Verifies that a compose result of false raises the DebugReportFailed event.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendDebugReport_ComposeFalse_RaisesDebugReportFailed()
    {
        _emailService.ComposeResult = false;
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.DebugCollectionEnabled = true;
        var failed = false;
        viewModel.DebugReportFailed += () =>
        {
            failed = true;
            return Task.CompletedTask;
        };

        await viewModel.SendDebugReportCommand.ExecuteAsync(null);

        Assert.True(failed);
        var entry = Assert.Single(_debugLogService.LoggedEntries, e => e.Level == DebugLogLevel.Error);
        Assert.Equal(DebugLogCategory.Report, entry.Category);
    }

    /// <summary>
    /// Verifies that a report service exception raises the DebugReportFailed event and
    /// records a report error in the session debug log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SendDebugReport_ComposeThrows_RaisesDebugReportFailed_AndLogs()
    {
        _emailService.ComposeException = new InvalidOperationException("mail client exploded");
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.DebugCollectionEnabled = true;
        var failed = false;
        viewModel.DebugReportFailed += () =>
        {
            failed = true;
            return Task.CompletedTask;
        };

        await viewModel.SendDebugReportCommand.ExecuteAsync(null);

        Assert.True(failed);
        var entry = Assert.Single(_debugLogService.LoggedEntries, e => e.Level == DebugLogLevel.Error);
        Assert.Equal(DebugLogCategory.Report, entry.Category);
        Assert.Contains("mail client exploded", entry.Details);
    }

    /// <summary>
    /// Verifies that the view model works without the optional debug services.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task WithoutDebugServices_SendIsDisabledAndToggleStillPersists()
    {
        var viewModel = CreateViewModel(includeDebugServices: false);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.DebugEmailSupported);
        Assert.False(viewModel.DebugSendEnabled);

        viewModel.DebugCollectionEnabled = true;
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).DebugCollectionEnabled);
        await viewModel.SendDebugReportCommand.ExecuteAsync(null);
    }
}
