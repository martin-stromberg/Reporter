// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private const int MinRetentionDays = 1;
    private const int MaxRetentionDays = 365;
    private const int DefaultRefreshIntervalMinutes = 30;
    private const int DefaultAutoMarkReadDelaySeconds = 5;

    private static readonly TimeSpan DefaultQuietHoursStart = new(22, 0, 0);
    private static readonly TimeSpan DefaultQuietHoursEnd = new(7, 0, 0);
    private static readonly TimeSpan RetentionPersistDebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly ISettingsRepository _settingsRepository;
    private readonly IKeywordRepository _keywordRepository;
    private readonly IAutoRefreshService _autoRefreshService;
    private readonly IAppThemeService _appThemeService;
    private readonly ILocalNotificationService? _localNotificationService;
    private readonly IDebugReportService? _debugReportService;
    private readonly IDebugLogService? _debugLogService;
    private readonly TimeProvider _timeProvider;

    private string _title = AppResources.PageTitleSettings;
    private Settings? _settings;
    private string _newKeywordText = string.Empty;
    private double _retentionDays = 30;
    private string _retentionDaysText;
    private bool _autoRefreshEnabled = true;
    private RefreshIntervalOption? _selectedRefreshInterval;
    private bool _refreshOnStartupEnabled = true;
    private SortOrderOption? _selectedSortOrder;
    private bool _autoMarkReadEnabled = true;
    private AutoMarkReadDelayOption? _selectedAutoMarkReadDelay;
    private bool _notificationsEnabled = true;
    private bool _notificationPermissionDenied;
    private bool _notificationPermissionNotDetermined;
    private bool _notificationSummaryEnabled;
    private bool _debugCollectionEnabled;
    private bool _quietHoursEnabled;
    private TimeSpan? _quietHoursStart;
    private TimeSpan? _quietHoursEnd;
    private ThemeOption? _selectedTheme;
    private LanguageOption? _selectedLanguage;
    private string? _loadedLanguage;
    private bool _languageRestartHintVisible;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private volatile bool _isLoading;
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
    /// <param name="localNotificationService">The platform notification service used to request authorization when notifications are turned on.</param>
    /// <param name="debugReportService">The debug report service used to send the collected debug report.</param>
    /// <param name="debugLogService">The session debug log service switched by the debug collection toggle.</param>
    public SettingsViewModel(
        ISettingsRepository settingsRepository,
        IKeywordRepository keywordRepository,
        IAutoRefreshService autoRefreshService,
        IAppThemeService appThemeService,
        TimeProvider? timeProvider = null,
        ILocalNotificationService? localNotificationService = null,
        IDebugReportService? debugReportService = null,
        IDebugLogService? debugLogService = null)
    {
        _settingsRepository = settingsRepository;
        _keywordRepository = keywordRepository;
        _autoRefreshService = autoRefreshService;
        _appThemeService = appThemeService;
        _localNotificationService = localNotificationService;
        _debugReportService = debugReportService;
        _debugLogService = debugLogService;
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
        LanguageOptions = new List<LanguageOption>
        {
            new() { Value = SettingsValues.LanguageSystem, Label = AppResources.SettingsLanguageSystem },
            new() { Value = SettingsValues.LanguageGerman, Label = AppResources.SettingsLanguageGerman },
            new() { Value = SettingsValues.LanguageEnglish, Label = AppResources.SettingsLanguageEnglish },
        };
        SortOrderOptions = new List<SortOrderOption>
        {
            new() { Value = SettingsValues.SortOrderDescending, Label = AppResources.SettingsSortOrderNewest },
            new() { Value = SettingsValues.SortOrderAscending, Label = AppResources.SettingsSortOrderOldest },
        };

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddKeywordCommand = new AsyncRelayCommand(AddKeywordAsync);
        RemoveKeywordCommand = new AsyncRelayCommand<Keyword>(RemoveKeywordAsync);
        SaveRetentionCommand = new RelayCommand(SaveRetention);
        RequestNotificationPermissionCommand = new AsyncRelayCommand(RequestNotificationAuthorizationAsync);
        SendDebugReportCommand = new AsyncRelayCommand(SendDebugReportAsync);
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
    /// Gets the command that requests the platform notification authorization,
    /// invoked from the not-yet-allowed hint row on the settings page.
    /// </summary>
    public AsyncRelayCommand RequestNotificationPermissionCommand { get; }

    /// <summary>
    /// Gets the command that collects the debug report and hands it to the system mail client.
    /// </summary>
    public AsyncRelayCommand SendDebugReportCommand { get; }

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
    /// Gets the selectable language options.
    /// </summary>
    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    /// <summary>
    /// Gets the selectable sort order options for the unread articles list.
    /// </summary>
    public IReadOnlyList<SortOrderOption> SortOrderOptions { get; }

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
        set
        {
            if (SetProperty(ref _newKeywordText, value))
            {
                HasError = false;
            }
        }
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
            var rounded = Math.Round(value);
            if (SetProperty(ref _retentionDays, rounded))
            {
                RetentionDaysText = FormatRetentionDays(rounded);
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
    /// Gets or sets a value indicating whether feeds are refreshed when the application starts.
    /// </summary>
    public bool RefreshOnStartupEnabled
    {
        get => _refreshOnStartupEnabled;
        set
        {
            if (SetProperty(ref _refreshOnStartupEnabled, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected sort order option for the unread articles list.
    /// </summary>
    public SortOrderOption? SelectedSortOrder
    {
        get => _selectedSortOrder;
        set
        {
            if (SetProperty(ref _selectedSortOrder, value))
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
    /// Occurs when the user turns notifications on while the system authorization
    /// for local notifications is denied. Subscribers (the page) can inform the
    /// user and offer a way to the system settings.
    /// </summary>
    public event Func<Task>? NotificationAuthorizationDenied;

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled.
    /// Turning this on requests the platform notification authorization in the
    /// context of the deliberate activation.
    /// </summary>
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (SetProperty(ref _notificationsEnabled, value))
            {
                if (!value)
                {
                    NotificationPermissionDenied = false;
                    NotificationPermissionNotDetermined = false;
                }

                OnPropertyChanged(nameof(NotificationControlsEnabled));
                PersistOnChange();
                if (value && !_isLoading)
                {
                    _ = RequestNotificationAuthorizationAsync();
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current platform supports local notifications.
    /// When <c>false</c>, the settings page disables the notification controls and
    /// shows a platform hint instead.
    /// </summary>
    public bool NotificationsSupported => _localNotificationService?.IsSupported == true;

    /// <summary>
    /// Gets a value indicating whether the notification detail controls (summary mode,
    /// quiet hours) are editable: the platform supports notifications and the main
    /// switch is on.
    /// </summary>
    public bool NotificationControlsEnabled => NotificationsSupported && NotificationsEnabled;

    /// <summary>
    /// Gets a value indicating whether notifications are switched on in the app but
    /// the system authorization for local notifications is currently denied.
    /// The settings page surfaces this state as a hint with a link to the system settings.
    /// </summary>
    public bool NotificationPermissionDenied
    {
        get => _notificationPermissionDenied;
        private set => SetProperty(ref _notificationPermissionDenied, value);
    }

    /// <summary>
    /// Gets a value indicating whether notifications are switched on in the app but
    /// the system authorization has not been requested yet (<c>NotDetermined</c>).
    /// The settings page surfaces this state as a neutral hint with an
    /// "allow notifications" action.
    /// </summary>
    public bool NotificationPermissionNotDetermined
    {
        get => _notificationPermissionNotDetermined;
        private set => SetProperty(ref _notificationPermissionNotDetermined, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether new items trigger a single summary
    /// notification per feed instead of one notification per article.
    /// </summary>
    public bool NotificationSummaryEnabled
    {
        get => _notificationSummaryEnabled;
        set
        {
            if (SetProperty(ref _notificationSummaryEnabled, value))
            {
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Occurs when sending the debug report is not possible or failed. Subscribers
    /// (the page) can inform the user.
    /// </summary>
    public event Func<Task>? DebugReportFailed;

    /// <summary>
    /// Gets or sets a value indicating whether the collection of debug information
    /// (session debug log) is enabled. Toggling the switch persists the value and
    /// switches the session debug log immediately.
    /// </summary>
    public bool DebugCollectionEnabled
    {
        get => _debugCollectionEnabled;
        set
        {
            if (SetProperty(ref _debugCollectionEnabled, value))
            {
                if (!_isLoading)
                {
                    _debugLogService?.SetEnabled(value);
                }

                OnPropertyChanged(nameof(DebugSendEnabled));
                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current platform supports composing e-mail.
    /// When <c>false</c>, the settings page disables the send action and shows a hint.
    /// </summary>
    public bool DebugEmailSupported => _debugReportService?.IsSupported == true;

    /// <summary>
    /// Gets a value indicating whether the send-debug-report action is enabled:
    /// the platform supports e-mail compose and debug collection is switched on.
    /// </summary>
    public bool DebugSendEnabled => DebugEmailSupported && DebugCollectionEnabled;

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
    /// Gets or sets the selected language option.
    /// </summary>
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                if (!_isLoading)
                {
                    LanguageRestartHintVisible = value?.Value != _loadedLanguage;
                }

                PersistOnChange();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the hint that a language change requires an
    /// app restart is visible. The hint is shown only after the selected language
    /// differs from the persisted value and is hidden again when the persisted
    /// language is reselected.
    /// </summary>
    public bool LanguageRestartHintVisible
    {
        get => _languageRestartHintVisible;
        private set => SetProperty(ref _languageRestartHintVisible, value);
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
        return string.Format(CultureInfo.CurrentCulture, AppResources.SettingsRetentionDaysFormat, (int)Math.Round(days));
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
            RefreshOnStartupEnabled = settings.RefreshOnStartupEnabled;
            SelectedSortOrder = SortOrderOptions.FirstOrDefault(o => o.Value == settings.UnreadSortOrder)
                ?? SortOrderOptions.First(o => o.Value == SettingsValues.SortOrderDescending);
            AutoMarkReadEnabled = SettingsValues.IsAutoMarkReadEnabled(settings.AutoMarkReadMode);
            SelectedAutoMarkReadDelay = AutoMarkReadDelayOptions.FirstOrDefault(o => o.Seconds == settings.AutoMarkReadDelaySeconds)
                ?? AutoMarkReadDelayOptions.First(o => o.Seconds == DefaultAutoMarkReadDelaySeconds);
            NotificationsEnabled = settings.NotificationsEnabled;
            NotificationSummaryEnabled = settings.NotificationSummaryEnabled;
            DebugCollectionEnabled = settings.DebugCollectionEnabled;
            QuietHoursStart = settings.QuietHoursStart ?? _quietHoursStart;
            QuietHoursEnd = settings.QuietHoursEnd ?? _quietHoursEnd;
            QuietHoursEnabled = settings.QuietHoursStart is not null || settings.QuietHoursEnd is not null;
            SelectedTheme = ThemeOptions.FirstOrDefault(o => o.Value == settings.Theme)
                ?? ThemeOptions.First(o => o.Value == SettingsValues.ThemeSystem);
            SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Value == settings.Language)
                ?? LanguageOptions.First(o => o.Value == SettingsValues.LanguageSystem);
            _loadedLanguage = SelectedLanguage?.Value;
            LanguageRestartHintVisible = false;

            Keywords.Clear();
            var keywords = await _keywordRepository.GetByFeedAsync(null);
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

        await RefreshNotificationPermissionAsync();
    }

    private void PersistOnChange()
    {
        if (_isLoading)
        {
            return;
        }

        _ = PersistAsync();
    }

    private async Task RefreshNotificationPermissionAsync()
    {
        if (_localNotificationService is null || !_localNotificationService.IsSupported || !NotificationsEnabled)
        {
            NotificationPermissionDenied = false;
            NotificationPermissionNotDetermined = false;
            return;
        }

        try
        {
            ApplyAuthorizationStatus(await _localNotificationService.GetAuthorizationStatusAsync());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to query notification authorization: {ex}");
            NotificationPermissionDenied = false;
            NotificationPermissionNotDetermined = false;
        }
    }

    private async Task RequestNotificationAuthorizationAsync()
    {
        if (_localNotificationService is null || !_localNotificationService.IsSupported)
        {
            return;
        }

        try
        {
            var granted = await _localNotificationService.RequestAuthorizationAsync();
            var status = granted
                ? NotificationAuthorizationStatus.Authorized
                : await _localNotificationService.GetAuthorizationStatusAsync();
            ApplyAuthorizationStatus(status);
            if (status == NotificationAuthorizationStatus.Denied && NotificationAuthorizationDenied is not null)
            {
                await NotificationAuthorizationDenied.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to request notification authorization: {ex}");
        }
    }

    private void ApplyAuthorizationStatus(NotificationAuthorizationStatus status)
    {
        NotificationPermissionDenied = status == NotificationAuthorizationStatus.Denied;
        NotificationPermissionNotDetermined = status == NotificationAuthorizationStatus.NotDetermined;
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
        var previous = Interlocked.Exchange(ref _retentionDebounceCts, cts);
        previous?.Dispose();
        _ = PersistRetentionDebouncedAsync(cts);
    }

    private void CancelRetentionDebounce()
    {
        var cts = Interlocked.Exchange(ref _retentionDebounceCts, null);
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // A completed debounced persist already disposed the source.
        }
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

        // Ownership is claimed atomically: only the still-current debounce may
        // persist, and CancelRetentionDebounce never cancels a disposed source.
        if (Interlocked.CompareExchange(ref _retentionDebounceCts, null, cts) != cts)
        {
            return;
        }

        cts.Dispose();

        if (_isLoading)
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
                NotificationSummaryEnabled = NotificationSummaryEnabled,
                QuietHoursStart = QuietHoursEnabled ? QuietHoursStart : null,
                QuietHoursEnd = QuietHoursEnabled ? QuietHoursEnd : null,
                AutoRefreshEnabled = AutoRefreshEnabled,
                RefreshIntervalMinutes = SelectedRefreshInterval?.Minutes ?? DefaultRefreshIntervalMinutes,
                RefreshOnStartupEnabled = RefreshOnStartupEnabled,
                UnreadSortOrder = SelectedSortOrder?.Value ?? SettingsValues.SortOrderDescending,
                Theme = SelectedTheme?.Value ?? SettingsValues.ThemeSystem,
                Language = SelectedLanguage?.Value ?? SettingsValues.LanguageSystem,
                DebugCollectionEnabled = DebugCollectionEnabled,
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
        if (!KeywordValidator.TryValidate(text, Keywords, out var validationError))
        {
            ErrorMessage = validationError;
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

    private async Task SendDebugReportAsync()
    {
        // The guard lives in the method instead of CanExecute because
        // IAsyncRelayCommand.ExecuteAsync does not evaluate CanExecute.
        if (_debugReportService is null || !DebugCollectionEnabled)
        {
            return;
        }

        try
        {
            var sent = await _debugReportService.SendReportAsync();
            if (!sent && DebugReportFailed is not null)
            {
                await DebugReportFailed.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send debug report: {ex}");
            if (_debugLogService is not null)
            {
                await _debugLogService.LogAsync(
                    DebugLogCategory.Report,
                    "Failed to send debug report",
                    ex.ToString(),
                    DebugLogLevel.Error);
            }

            if (DebugReportFailed is not null)
            {
                await DebugReportFailed.Invoke();
            }
        }
    }
}
