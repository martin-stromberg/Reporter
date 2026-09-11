using Microsoft.Extensions.Time.Testing;
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
    private readonly FakeTimeProvider _timeProvider;
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
        _timeProvider = new FakeTimeProvider();
        _viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService, _timeProvider);
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
    /// Verifies that a retention-days change without drag completion (e.g. keyboard input
    /// on the slider) is persisted after the debounce delay instead of being silently
    /// dropped (usability finding: value changes were only persisted via
    /// <see cref="SettingsViewModel.SaveRetentionCommand"/>).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.RetentionDays = 42;
        await Task.Delay(50);

        var before = await _settingsRepository.GetAsync();
        Assert.NotEqual(42, before.RetentionDays);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(500));
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).RetentionDays == 42);

        Assert.Equal(42, _viewModel.RetentionDays);
    }

    /// <summary>
    /// Verifies that rapid consecutive retention-days changes within the debounce window
    /// result in a single persist of the last value.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RetentionDays_RapidChanges_PersistOnlyLastValue()
    {
        var recordingRepository = new RecordingSettingsRepository(_settingsRepository);
        var viewModel = new SettingsViewModel(recordingRepository, _keywordRepository, _autoRefreshService, _appThemeService, _timeProvider);
        await viewModel.LoadCommand.ExecuteAsync(null);
        recordingRepository.SavedRetentionDays.Clear();

        viewModel.RetentionDays = 10;
        viewModel.RetentionDays = 20;
        viewModel.RetentionDays = 42;
        _timeProvider.Advance(TimeSpan.FromMilliseconds(500));

        await TestWaitHelper.WaitUntilAsync(() => recordingRepository.SavedRetentionDays.Count == 1);

        Assert.Equal(42, recordingRepository.SavedRetentionDays[0]);
    }

    /// <summary>
    /// Verifies that the drag-completed path still persists immediately and cancels a
    /// pending debounced persist so the value is not saved twice.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RetentionDays_DragCompleted_PersistsImmediatelyAndCancelsDebounce()
    {
        var recordingRepository = new RecordingSettingsRepository(_settingsRepository);
        var viewModel = new SettingsViewModel(recordingRepository, _keywordRepository, _autoRefreshService, _appThemeService, _timeProvider);
        await viewModel.LoadCommand.ExecuteAsync(null);
        recordingRepository.SavedRetentionDays.Clear();

        viewModel.RetentionDays = 42;
        viewModel.SaveRetentionCommand.Execute(null);
        await TestWaitHelper.WaitUntilAsync(() => recordingRepository.SavedRetentionDays.Count == 1);

        _timeProvider.Advance(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Assert.Single(recordingRepository.SavedRetentionDays);
        Assert.Equal(42, recordingRepository.SavedRetentionDays[0]);
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
    /// Verifies that turning the quiet-hours switch off persists <c>null</c> for both
    /// quiet-hours fields so a once-set quiet period can be removed again, while the
    /// previously displayed times stay in the view model for the running session
    /// (usability finding: an accidental tap must not destroy the configured times).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task QuietHoursEnabled_TurnedOff_PersistsNull()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            QuietHoursStart = new TimeSpan(23, 30, 0),
            QuietHoursEnd = new TimeSpan(6, 15, 0),
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            Theme = settings.Theme,
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.True(_viewModel.QuietHoursEnabled);

        _viewModel.QuietHoursEnabled = false;
        await TestWaitHelper.WaitUntilAsync(async () =>
        {
            var s = await _settingsRepository.GetAsync();
            return s.QuietHoursStart is null && s.QuietHoursEnd is null;
        });

        Assert.Equal(new TimeSpan(23, 30, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(6, 15, 0), _viewModel.QuietHoursEnd);
        var persisted = await _settingsRepository.GetAsync();
        Assert.Null(persisted.QuietHoursStart);
        Assert.Null(persisted.QuietHoursEnd);
    }

    /// <summary>
    /// Verifies that turning the quiet-hours switch off and on again within the same
    /// session restores the previously displayed times instead of the fixed defaults
    /// and persists them once re-enabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task QuietHoursEnabled_ToggledOffAndOn_RestoresSessionValues()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            QuietHoursStart = new TimeSpan(23, 30, 0),
            QuietHoursEnd = new TimeSpan(6, 15, 0),
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            Theme = settings.Theme,
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.QuietHoursEnabled = false;
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).QuietHoursStart is null);

        _viewModel.QuietHoursEnabled = true;
        await TestWaitHelper.WaitUntilAsync(async () =>
        {
            var s = await _settingsRepository.GetAsync();
            return s.QuietHoursStart == new TimeSpan(23, 30, 0) && s.QuietHoursEnd == new TimeSpan(6, 15, 0);
        });

        Assert.Equal(new TimeSpan(23, 30, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(6, 15, 0), _viewModel.QuietHoursEnd);
    }

    /// <summary>
    /// Verifies that reloading the settings while quiet hours are turned off keeps the
    /// session-retained times in the view model instead of falling back to unset values.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task QuietHoursEnabled_TurnedOff_ReloadKeepsSessionValues()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            QuietHoursStart = new TimeSpan(23, 30, 0),
            QuietHoursEnd = new TimeSpan(6, 15, 0),
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            Theme = settings.Theme,
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.QuietHoursEnabled = false;
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).QuietHoursStart is null);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(_viewModel.QuietHoursEnabled);
        Assert.Equal(new TimeSpan(23, 30, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(6, 15, 0), _viewModel.QuietHoursEnd);
    }

    /// <summary>
    /// Verifies that turning the quiet-hours switch on applies the default quiet period
    /// (22:00–07:00) and persists it immediately.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task QuietHoursEnabled_TurnedOn_AppliesDefaults()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        Assert.False(_viewModel.QuietHoursEnabled);

        _viewModel.QuietHoursEnabled = true;
        await TestWaitHelper.WaitUntilAsync(async () =>
        {
            var s = await _settingsRepository.GetAsync();
            return s.QuietHoursStart is not null && s.QuietHoursEnd is not null;
        });

        Assert.Equal(new TimeSpan(22, 0, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(7, 0, 0), _viewModel.QuietHoursEnd);
        var persisted = await _settingsRepository.GetAsync();
        Assert.Equal(new TimeSpan(22, 0, 0), persisted.QuietHoursStart);
        Assert.Equal(new TimeSpan(7, 0, 0), persisted.QuietHoursEnd);
    }

    /// <summary>
    /// Verifies that turning the quiet-hours switch on keeps already configured times
    /// instead of overwriting them with the defaults.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task QuietHoursEnabled_TurnedOn_KeepsExistingValues()
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            QuietHoursStart = new TimeSpan(23, 30, 0),
            QuietHoursEnd = new TimeSpan(6, 15, 0),
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            Theme = settings.Theme,
        });
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.QuietHoursEnabled = false;
        _viewModel.QuietHoursStart = new TimeSpan(21, 0, 0);
        _viewModel.QuietHoursEnd = new TimeSpan(5, 0, 0);
        _viewModel.QuietHoursEnabled = true;

        Assert.Equal(new TimeSpan(21, 0, 0), _viewModel.QuietHoursStart);
        Assert.Equal(new TimeSpan(5, 0, 0), _viewModel.QuietHoursEnd);
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

    private sealed class RecordingSettingsRepository : ISettingsRepository
    {
        private readonly ISettingsRepository _inner;

        public RecordingSettingsRepository(ISettingsRepository inner)
        {
            _inner = inner;
        }

        public List<int> SavedRetentionDays { get; } = new();

        public Task<Settings> GetAsync(CancellationToken cancellationToken = default)
        {
            return _inner.GetAsync(cancellationToken);
        }

        public async Task SaveAsync(Settings settings)
        {
            SavedRetentionDays.Add(settings.RetentionDays);
            await _inner.SaveAsync(settings);
        }
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
