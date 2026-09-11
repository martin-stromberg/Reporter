using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the load behavior of the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests_Load : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_Load"/> class.
    /// </summary>
    public SettingsViewModelTests_Load()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _autoRefreshService = new FakeAutoRefreshService();
        _appThemeService = new FakeAppThemeService();
        _viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that LoadCommand populates all option properties from the persisted settings and keywords.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PopulatesAllOptions()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = 42,
            AutoMarkReadMode = "off",
            AutoMarkReadDelaySeconds = 3,
            NotificationsEnabled = false,
            NotificationSummaryEnabled = true,
            QuietHoursStart = new TimeSpan(22, 0, 0),
            QuietHoursEnd = new TimeSpan(7, 0, 0),
            AutoRefreshEnabled = true,
            RefreshIntervalMinutes = 60,
            Theme = "dark",
        });
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Werbung" });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(42, _viewModel.RetentionDays);
        Assert.True(_viewModel.AutoRefreshEnabled);
        Assert.Equal(60, _viewModel.SelectedRefreshInterval?.Minutes);
        Assert.False(_viewModel.AutoMarkReadEnabled);
        Assert.Equal(3, _viewModel.SelectedAutoMarkReadDelay?.Seconds);
        Assert.False(_viewModel.NotificationsEnabled);
        Assert.True(_viewModel.NotificationSummaryEnabled);
        Assert.True(_viewModel.QuietHoursEnabled);
        Assert.Equal(new TimeSpan(22, 0, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(7, 0, 0), _viewModel.QuietHoursEnd);
        Assert.Equal("dark", _viewModel.SelectedTheme?.Value);
        Assert.Single(_viewModel.Keywords);
        Assert.Equal("Werbung", _viewModel.Keywords[0].KeywordText);
        Assert.False(_viewModel.HasError);
    }

    /// <summary>
    /// Verifies that invalid persisted values fall back to defaults when loading.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_InvalidPersistedValues_UsesFallbacks()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = 7,
            NotificationsEnabled = settings.NotificationsEnabled,
            NotificationSummaryEnabled = settings.NotificationSummaryEnabled,
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = 45,
            Theme = "sepia",
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(30, _viewModel.SelectedRefreshInterval?.Minutes);
        Assert.Equal(5, _viewModel.SelectedAutoMarkReadDelay?.Seconds);
        Assert.Equal("system", _viewModel.SelectedTheme?.Value);
    }

    /// <summary>
    /// Verifies that the persisted notification summary flag is loaded into the view model.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PopulatesNotificationSummaryEnabled()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.NotificationSummaryEnabled);
    }

    /// <summary>
    /// Verifies that loading with notifications enabled but denied system authorization
    /// surfaces the denied state via <see cref="SettingsViewModel.NotificationPermissionDenied"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PermissionDenied_SetsNotificationPermissionDenied()
    {
        var localNotifications = new FakeLocalNotificationService { AuthorizationStatus = NotificationAuthorizationStatus.Denied };
        var viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: localNotifications);
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationsEnabled: true);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.NotificationsEnabled);
        Assert.True(viewModel.NotificationPermissionDenied);
        Assert.False(viewModel.NotificationPermissionNotDetermined);
    }

    /// <summary>
    /// Verifies that loading with notifications enabled and granted system authorization
    /// leaves <see cref="SettingsViewModel.NotificationPermissionDenied"/> cleared.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PermissionGranted_ClearsNotificationPermissionDenied()
    {
        var localNotifications = new FakeLocalNotificationService { AuthorizationStatus = NotificationAuthorizationStatus.Authorized };
        var viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: localNotifications);
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationsEnabled: true);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.NotificationsEnabled);
        Assert.False(viewModel.NotificationPermissionDenied);
        Assert.False(viewModel.NotificationPermissionNotDetermined);
    }

    /// <summary>
    /// Verifies that loading with notifications enabled and a not-yet-requested system
    /// authorization surfaces the neutral not-determined state instead of the denied hint
    /// (usability finding: <c>NotDetermined</c> must not look like a refusal — on iOS the
    /// system settings entry does not even exist before the first request).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_PermissionNotDetermined_SetsNotificationPermissionNotDetermined()
    {
        var localNotifications = new FakeLocalNotificationService { AuthorizationStatus = NotificationAuthorizationStatus.NotDetermined };
        var viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: localNotifications);
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationsEnabled: true);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.NotificationsEnabled);
        Assert.True(viewModel.NotificationPermissionNotDetermined);
        Assert.False(viewModel.NotificationPermissionDenied);
    }

    /// <summary>
    /// Verifies that <see cref="SettingsViewModel.NotificationsSupported"/> reflects the
    /// platform support reported by the notification service (or its absence).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_NotificationsSupported_ReflectsPlatformSupport()
    {
        var supported = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: new FakeLocalNotificationService());
        var unsupported = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: new FakeLocalNotificationService { IsSupported = false });
        var withoutService = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService);

        await supported.LoadCommand.ExecuteAsync(null);

        Assert.True(supported.NotificationsSupported);
        Assert.False(unsupported.NotificationsSupported);
        Assert.False(withoutService.NotificationsSupported);
    }

    /// <summary>
    /// Verifies that the authorization status is not surfaced when notifications are
    /// switched off in the app.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_NotificationsDisabled_HidesNotificationPermissionDenied()
    {
        var localNotifications = new FakeLocalNotificationService { AuthorizationStatus = NotificationAuthorizationStatus.Denied };
        var viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, localNotificationService: localNotifications);
        await TestSettingsHelper.SaveAsync(_settingsRepository, notificationsEnabled: false);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.NotificationsEnabled);
        Assert.False(viewModel.NotificationPermissionDenied);
    }

    /// <summary>
    /// Verifies that persisted settings without quiet hours load with the quiet-hours
    /// switch turned off and both time values unset.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Load_WithoutQuietHours_QuietHoursDisabled()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(_viewModel.QuietHoursEnabled);
        Assert.Null(_viewModel.QuietHoursStart);
        Assert.Null(_viewModel.QuietHoursEnd);
    }

    /// <summary>
    /// Verifies that the theme options expose exactly the persisted values "system",
    /// "light" and "dark" defined by <see cref="SettingsValues"/>.
    /// </summary>
    [Fact]
    public void ThemeOptions_ExposePersistedValues()
    {
        Assert.Equal(
            new[] { "system", "light", "dark" },
            _viewModel.ThemeOptions.Select(o => o.Value).ToArray());
    }
}
