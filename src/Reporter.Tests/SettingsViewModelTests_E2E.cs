using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains end-to-end tests for the <see cref="SettingsViewModel"/> class covering the full
/// flow from the view model over the real repositories to the database.
/// </summary>
public class SettingsViewModelTests_E2E : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_E2E"/> class.
    /// </summary>
    public SettingsViewModelTests_E2E()
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

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService);
    }

    /// <summary>
    /// Verifies the end-to-end flow: change options, persist immediately and reload them on the next load.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_ChangeSettings_PersistRoundtrip()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.RetentionDays = 90;
        _viewModel.SaveRetentionCommand.Execute(null);
        _viewModel.NotificationsEnabled = false;
        _viewModel.QuietHoursEnabled = true;
        _viewModel.QuietHoursStart = new TimeSpan(22, 0, 0);
        _viewModel.QuietHoursEnd = new TimeSpan(7, 0, 0);
        _viewModel.AutoRefreshEnabled = false;
        _viewModel.SelectedTheme = _viewModel.ThemeOptions.First(o => o.Value == "light");
        _viewModel.AutoMarkReadEnabled = false;

        await TestWaitHelper.WaitUntilAsync(async () =>
        {
            var s = await _settingsRepository.GetAsync();
            return s.RetentionDays == 90
                && !s.NotificationsEnabled
                && s.QuietHoursStart == new TimeSpan(22, 0, 0)
                && s.QuietHoursEnd == new TimeSpan(7, 0, 0)
                && !s.AutoRefreshEnabled
                && s.Theme == "light"
                && s.AutoMarkReadMode == "off";
        });

        var reloaded = CreateViewModel();
        await reloaded.LoadCommand.ExecuteAsync(null);

        Assert.Equal(90, reloaded.RetentionDays);
        Assert.False(reloaded.NotificationsEnabled);
        Assert.Equal(new TimeSpan(22, 0, 0), reloaded.QuietHoursStart);
        Assert.Equal(new TimeSpan(7, 0, 0), reloaded.QuietHoursEnd);
        Assert.False(reloaded.AutoRefreshEnabled);
        Assert.Equal("light", reloaded.SelectedTheme?.Value);
        Assert.False(reloaded.AutoMarkReadEnabled);
        Assert.Contains("light", _appThemeService.AppliedThemes);
        Assert.Contains(_autoRefreshService.AppliedSettings, s => !s.AutoRefreshEnabled);
    }

    /// <summary>
    /// Verifies the end-to-end flow: add a keyword, reload it from the database and remove it again.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordAddRemove_Persists()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "Gewinnspiel";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        var reloaded = CreateViewModel();
        await reloaded.LoadCommand.ExecuteAsync(null);

        Assert.Single(reloaded.Keywords);
        Assert.Equal("Gewinnspiel", reloaded.Keywords[0].KeywordText);

        await reloaded.RemoveKeywordCommand.ExecuteAsync(reloaded.Keywords[0]);

        var reloadedAgain = CreateViewModel();
        await reloadedAgain.LoadCommand.ExecuteAsync(null);

        Assert.Empty(reloadedAgain.Keywords);
        Assert.Empty(await _keywordRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies the end-to-end flow: an empty keyword is rejected with a visible error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordEmpty_Rejected()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "Werbung";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        _viewModel.NewKeywordText = "   ";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.ErrorKeywordEmpty, _viewModel.ErrorMessage);
        Assert.Single(await _keywordRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies the end-to-end flow: a duplicate keyword is rejected with a visible error.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task E2E_KeywordDuplicate_Rejected()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "Werbung";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        _viewModel.NewKeywordText = "WERBUNG";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.ErrorKeywordDuplicate, _viewModel.ErrorMessage);
        Assert.Single(await _keywordRepository.GetAllAsync());
    }
}
