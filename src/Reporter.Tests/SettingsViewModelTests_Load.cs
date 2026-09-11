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
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = 45,
            Theme = "sepia",
        });

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(30, _viewModel.SelectedRefreshInterval?.Minutes);
        Assert.Equal(5, _viewModel.SelectedAutoMarkReadDelay?.Seconds);
        Assert.Equal("system", _viewModel.SelectedTheme?.Value);
    }
}
