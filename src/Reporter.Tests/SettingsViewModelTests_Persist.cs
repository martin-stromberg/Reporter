using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the immediate persist behavior of the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests_Persist : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_Persist"/> class.
    /// </summary>
    public SettingsViewModelTests_Persist()
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
    /// Verifies that changing an option property persists the new value immediately.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task PropertyChange_PersistsImmediately()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.NotificationsEnabled = false;
        await TestWaitHelper.WaitUntilAsync(async () => !(await _settingsRepository.GetAsync()).NotificationsEnabled);

        var settings = await _settingsRepository.GetAsync();
        Assert.False(settings.NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that out-of-range retention days are clamped and persisted on drag completion.
    /// </summary>
    /// <param name="input">The slider value.</param>
    /// <param name="expected">The expected clamped value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(400, 365)]
    public async Task RetentionDays_OutOfRange_Clamped(double input, int expected)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.RetentionDays = input;
        _viewModel.SaveRetentionCommand.Execute(null);
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).RetentionDays == expected);

        Assert.Equal(expected, _viewModel.RetentionDays);
        var settings = await _settingsRepository.GetAsync();
        Assert.Equal(expected, settings.RetentionDays);
    }

    /// <summary>
    /// Verifies that changing the theme applies it through the theme service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ThemeChange_AppliesTheme()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectedTheme = _viewModel.ThemeOptions.First(o => o.Value == "dark");
        await TestWaitHelper.WaitUntilAsync(() => _appThemeService.AppliedThemes.Contains("dark"));

        Assert.Contains("dark", _appThemeService.AppliedThemes);
    }

    /// <summary>
    /// Verifies that changing the auto-refresh options applies the settings through the auto refresh service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AutoRefreshChange_AppliesSettings()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.AutoRefreshEnabled = false;
        await TestWaitHelper.WaitUntilAsync(() => _autoRefreshService.AppliedSettings.Any(s => !s.AutoRefreshEnabled));

        _viewModel.AutoRefreshEnabled = true;
        _viewModel.SelectedRefreshInterval = _viewModel.RefreshIntervalOptions.First(o => o.Minutes == 60);
        await TestWaitHelper.WaitUntilAsync(() => _autoRefreshService.AppliedSettings.Any(s => s.AutoRefreshEnabled && s.RefreshIntervalMinutes == 60));

        var last = _autoRefreshService.AppliedSettings.Last();
        Assert.True(last.AutoRefreshEnabled);
        Assert.Equal(60, last.RefreshIntervalMinutes);
    }

    /// <summary>
    /// Verifies that a follow-up persist queued while a previous save is still running
    /// compares against the just-saved settings instead of a stale snapshot, so the
    /// theme is not applied twice (review finding: snapshot was built outside the lock).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task Persist_QueuedBehindRunningSave_AppliesThemeOnce()
    {
        var saveGate = new TaskCompletionSource<bool>();
        var gatedRepository = new GatedSettingsRepository(_settingsRepository) { SaveGate = saveGate.Task };
        var viewModel = new SettingsViewModel(gatedRepository, _keywordRepository, _autoRefreshService, _appThemeService);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedTheme = viewModel.ThemeOptions.First(o => o.Value == "dark");
        viewModel.NotificationsEnabled = false;
        saveGate.SetResult(true);

        await TestWaitHelper.WaitUntilAsync(async () => !(await _settingsRepository.GetAsync()).NotificationsEnabled);

        Assert.Equal(1, _appThemeService.AppliedThemes.Count(t => t == "dark"));
    }

    private sealed class GatedSettingsRepository : ISettingsRepository
    {
        private readonly ISettingsRepository _inner;

        public GatedSettingsRepository(ISettingsRepository inner)
        {
            _inner = inner;
        }

        public Task? SaveGate { get; set; }

        public Task<Settings> GetAsync(CancellationToken cancellationToken = default)
        {
            return _inner.GetAsync(cancellationToken);
        }

        public async Task SaveAsync(Settings settings)
        {
            if (SaveGate is not null)
            {
                await SaveGate;
            }

            await _inner.SaveAsync(settings);
        }
    }
}
