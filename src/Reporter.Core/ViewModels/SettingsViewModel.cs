using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the settings page.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private const int MinRetentionDays = 1;
    private const int MaxRetentionDays = 365;
    private const int MaxKeywordLength = 500;
    private const int DefaultRefreshIntervalMinutes = 30;
    private const int DefaultAutoMarkReadDelaySeconds = 5;

    private static readonly TimeSpan DefaultQuietHoursStart = new(22, 0, 0);
    private static readonly TimeSpan DefaultQuietHoursEnd = new(7, 0, 0);
    private static readonly TimeSpan RetentionPersistDebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly ISettingsRepository _settingsRepository;
    private readonly IKeywordRepository _keywordRepository;
    private readonly IAutoRefreshService _autoRefreshService;
    private readonly IAppThemeService _appThemeService;
    private readonly TimeProvider _timeProvider;

    private string _title = AppResources.PageTitleSettings;
    private Settings? _settings;
    private string _newKeywordText = string.Empty;
    private double _retentionDays = 30;
    private string _retentionDaysText;
    private bool _autoRefreshEnabled = true;
    private RefreshIntervalOption? _selectedRefreshInterval;
    private bool _autoMarkReadEnabled = true;
    private AutoMarkReadDelayOption? _selectedAutoMarkReadDelay;
    private bool _notificationsEnabled = true;
    private bool _quietHoursEnabled;
    private TimeSpan? _quietHoursStart;
    private TimeSpan? _quietHoursEnd;
    private ThemeOption? _selectedTheme;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private readonly SemaphoreSlim _persistLock = new(1, 1);
    private CancellationTokenSource? _retentionDebounceCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="keywordRepository">The keyword repository.</param>
    /// <param name="autoRefreshService">The auto refresh service.</param>
    /// <param name="appThemeService">The app theme service.</param>
    /// <param name="timeProvider">The time provider used for the retention-days persist debounce.</param>
    public SettingsViewModel(
        ISettingsRepository settingsRepository,
        IKeywordRepository keywordRepository,
        IAutoRefreshService autoRefreshService,
        IAppThemeService appThemeService,
        TimeProvider? timeProvider = null)
    {
        _settingsRepository = settingsRepository;
        _keywordRepository = keywordRepository;
        _autoRefreshService = autoRefreshService;
        _appThemeService = appThemeService;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _retentionDaysText = FormatRetentionDays(_retentionDays);

        RefreshIntervalOptions = new List<RefreshIntervalOption>
        {
            new() { Minutes = 15, Label = AppResources.SettingsInterval15Min },
            new() { Minutes = 30, Label = AppResources.SettingsInterval30Min },
            new() { Minutes = 60, Label = AppResources.SettingsIntervalHourly },
            new() { Minutes = 240, Label = AppResources.SettingsInterval4Hours },
        };
        AutoMarkReadDelayOptions = new List<AutoMarkReadDelayOption>
        {
            new() { Seconds = 0, Label = AppResources.SettingsDelayImmediate },
            new() { Seconds = 1, Label = AppResources.SettingsDelay1s },
            new() { Seconds = 3, Label = AppResources.SettingsDelay3s },
            new() { Seconds = 5, Label = AppResources.SettingsDelay5s },
        };
        ThemeOptions = new List<ThemeOption>
        {
            new() { Value = SettingsValues.ThemeSystem, Label = AppResources.SettingsThemeSystem },
            new() { Value = SettingsValues.ThemeLight, Label = AppResources.SettingsThemeLight },
            new() { Value = SettingsValues.ThemeDark, Label = AppResources.SettingsThemeDark },
        };

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddKeywordCommand = new AsyncRelayCommand(AddKeywordAsync);
        RemoveKeywordCommand = new AsyncRelayCommand<Keyword>(RemoveKeywordAsync);
        SaveRetentionCommand = new RelayCommand(SaveRetention);
    }

    /// <summary>
    /// Gets the command that loads the application settings.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that adds a new keyword filter.
    /// </summary>
    public AsyncRelayCommand AddKeywordCommand { get; }

    /// <summary>
    /// Gets the command that removes a keyword filter.
    /// </summary>
    public AsyncRelayCommand<Keyword> RemoveKeywordCommand { get; }

    /// <summary>
    /// Gets the command that persists the retention days value after the slider drag completed.
    /// </summary>
    public RelayCommand SaveRetentionCommand { get; }

    /// <summary>
    /// Gets the configured keyword filters.
    /// </summary>
    /// <value>The configured keyword filters.</value>
    public ObservableCollection<Keyword> Keywords { get; } = new();

    /// <summary>
    /// Gets the selectable refresh interval options.
    /// </summary>
    public IReadOnlyList<RefreshIntervalOption> RefreshIntervalOptions { get; }

    /// <summary>
    /// Gets the selectable auto-mark-as-read delay options.
    /// </summary>
    public IReadOnlyList<AutoMarkReadDelayOption> AutoMarkReadDelayOptions { get; }

    /// <summary>
    /// Gets the selectable theme options.
    /// </summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the application settings.
    /// </summary>
    public Settings? Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    /// <summary>
    /// Gets or sets the text of the keyword to add.
    /// </summary>
    public string NewKeywordText
    {
        get => _newKeywordText;
        set => SetProperty(ref _newKeywordText, value);
    }

    /// <summary>
    /// Gets or sets the retention period in days shown by the slider.
    /// Changes are persisted after a short debounce so adjustments without
    /// drag completion (e.g. keyboard input) are saved as well.
    /// </summary>
    public double RetentionDays
    {
        get => _retentionDays;
        set
        {
            if (SetProperty(ref _retentionDays, value))
            {
                RetentionDaysText = FormatRetentionDays(value);
                ScheduleRetentionPersist();
            }
        }
    }

    /// <summary>
    /// Gets or sets the localized display text for the current retention period.
    /// </summary>
    public string RetentionDaysText
    {
        get => _retentionDaysText;
        private set => SetProperty(ref _retentionDaysText, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether automatic background refresh is enabled.
    /// </summary>
    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            if (SetProperty(ref _autoRefreshEnabled, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected refresh interval option.
    /// </summary>
    public RefreshIntervalOption? SelectedRefreshInterval
    {
        get => _selectedRefreshInterval;
        set
        {
            if (SetProperty(ref _selectedRefreshInterval, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether articles are marked as read automatically.
    /// </summary>
    public bool AutoMarkReadEnabled
    {
        get => _autoMarkReadEnabled;
        set
        {
            if (SetProperty(ref _autoMarkReadEnabled, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected auto-mark-as-read delay option.
    /// </summary>
    public AutoMarkReadDelayOption? SelectedAutoMarkReadDelay
    {
        get => _selectedAutoMarkReadDelay;
        set
        {
            if (SetProperty(ref _selectedAutoMarkReadDelay, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether push notifications are enabled.
    /// </summary>
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (SetProperty(ref _notificationsEnabled, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether quiet hours are active.
    /// Turning this off persists <c>null</c> for the quiet-hours times while the displayed
    /// <see cref="QuietHoursStart"/> and <see cref="QuietHoursEnd"/> values are kept for the
    /// running session; turning it on restores those values or applies default times when
    /// none were set.
    /// </summary>
    public bool QuietHoursEnabled
    {
        get => _quietHoursEnabled;
        set
        {
            if (SetProperty(ref _quietHoursEnabled, value))
            {
                if (value)
                {
                    _quietHoursStart ??= DefaultQuietHoursStart;
                    _quietHoursEnd ??= DefaultQuietHoursEnd;

                    OnPropertyChanged(nameof(QuietHoursStart));
                    OnPropertyChanged(nameof(QuietHoursEnd));
                }

                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the start of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursStart
    {
        get => _quietHoursStart;
        set
        {
            if (SetProperty(ref _quietHoursStart, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the end of quiet hours.
    /// </summary>
    public TimeSpan? QuietHoursEnd
    {
        get => _quietHoursEnd;
        set
        {
            if (SetProperty(ref _quietHoursEnd, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected theme option.
    /// </summary>
    public ThemeOption? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a validation error is present.
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    private static string FormatRetentionDays(double days)
    {
        return string.Format(CultureInfo.CurrentCulture, AppResources.SettingsRetentionDaysFormat, (int)days);
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            var settings = await _settingsRepository.GetAsync();
            Settings = settings;

            RetentionDays = Math.Clamp(settings.RetentionDays, MinRetentionDays, MaxRetentionDays);
            AutoRefreshEnabled = settings.AutoRefreshEnabled;
            SelectedRefreshInterval = RefreshIntervalOptions.FirstOrDefault(o => o.Minutes == settings.RefreshIntervalMinutes)
                ?? RefreshIntervalOptions.First(o => o.Minutes == DefaultRefreshIntervalMinutes);
            AutoMarkReadEnabled = SettingsValues.IsAutoMarkReadEnabled(settings.AutoMarkReadMode);
            SelectedAutoMarkReadDelay = AutoMarkReadDelayOptions.FirstOrDefault(o => o.Seconds == settings.AutoMarkReadDelaySeconds)
                ?? AutoMarkReadDelayOptions.First(o => o.Seconds == DefaultAutoMarkReadDelaySeconds);
            NotificationsEnabled = settings.NotificationsEnabled;
            QuietHoursStart = settings.QuietHoursStart ?? _quietHoursStart;
            QuietHoursEnd = settings.QuietHoursEnd ?? _quietHoursEnd;
            QuietHoursEnabled = settings.QuietHoursStart is not null || settings.QuietHoursEnd is not null;
            SelectedTheme = ThemeOptions.FirstOrDefault(o => o.Value == settings.Theme)
                ?? ThemeOptions.First(o => o.Value == SettingsValues.ThemeSystem);

            Keywords.Clear();
            var keywords = await _keywordRepository.GetAllAsync();
            foreach (var keyword in keywords)
            {
                Keywords.Add(keyword);
            }

            HasError = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void PersistOnChange()
    {
        if (_isLoading)
        {
            return;
        }

        _ = PersistAsync();
    }

    private void SaveRetention()
    {
        RetentionDays = Math.Clamp((int)Math.Round(RetentionDays), MinRetentionDays, MaxRetentionDays);
        CancelRetentionDebounce();
        _ = PersistAsync();
    }

    private void ScheduleRetentionPersist()
    {
        if (_isLoading)
        {
            return;
        }

        CancelRetentionDebounce();
        var cts = new CancellationTokenSource();
        _retentionDebounceCts = cts;
        _ = PersistRetentionDebouncedAsync(cts);
    }

    private void CancelRetentionDebounce()
    {
        var cts = _retentionDebounceCts;
        _retentionDebounceCts = null;
        cts?.Cancel();
        cts?.Dispose();
    }

    private async Task PersistRetentionDebouncedAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(RetentionPersistDebounceDelay, _timeProvider, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (_isLoading || cts.IsCancellationRequested)
        {
            return;
        }

        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await _persistLock.WaitAsync();
        try
        {
            var previous = _settings;
            var updated = new Settings
            {
                Id = previous?.Id ?? Settings.DefaultId,
                RetentionDays = Math.Clamp((int)Math.Round(RetentionDays), MinRetentionDays, MaxRetentionDays),
                AutoMarkReadMode = AutoMarkReadEnabled ? SettingsValues.AutoMarkReadOnOpen : SettingsValues.AutoMarkReadOff,
                AutoMarkReadDelaySeconds = SelectedAutoMarkReadDelay?.Seconds ?? DefaultAutoMarkReadDelaySeconds,
                NotificationsEnabled = NotificationsEnabled,
                QuietHoursStart = QuietHoursEnabled ? QuietHoursStart : null,
                QuietHoursEnd = QuietHoursEnabled ? QuietHoursEnd : null,
                AutoRefreshEnabled = AutoRefreshEnabled,
                RefreshIntervalMinutes = SelectedRefreshInterval?.Minutes ?? DefaultRefreshIntervalMinutes,
                Theme = SelectedTheme?.Value ?? SettingsValues.ThemeSystem,
            };

            await _settingsRepository.SaveAsync(updated);
            Settings = updated;

            if (previous is null || previous.Theme != updated.Theme)
            {
                _appThemeService.ApplyTheme(updated.Theme);
            }

            if (previous is null || previous.AutoRefreshEnabled != updated.AutoRefreshEnabled || previous.RefreshIntervalMinutes != updated.RefreshIntervalMinutes)
            {
                await _autoRefreshService.ApplySettingsAsync(updated);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex}");
        }
        finally
        {
            _persistLock.Release();
        }
    }

    private async Task AddKeywordAsync()
    {
        var text = NewKeywordText?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            ErrorMessage = AppResources.ErrorKeywordEmpty;
            HasError = true;
            return;
        }

        if (text.Length > MaxKeywordLength)
        {
            ErrorMessage = AppResources.ErrorKeywordTooLong;
            HasError = true;
            return;
        }

        if (Keywords.Any(k => string.Equals(k.KeywordText, text, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = AppResources.ErrorKeywordDuplicate;
            HasError = true;
            return;
        }

        var keyword = new Keyword { Id = Guid.NewGuid(), KeywordText = text };
        try
        {
            await _keywordRepository.AddAsync(keyword);
            Keywords.Add(keyword);
            NewKeywordText = string.Empty;
            HasError = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add keyword: {ex}");
        }
    }

    private async Task RemoveKeywordAsync(Keyword? keyword)
    {
        if (keyword is null)
        {
            return;
        }

        try
        {
            await _keywordRepository.DeleteAsync(keyword.Id);
            Keywords.Remove(keyword);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove keyword: {ex}");
        }
    }
}
