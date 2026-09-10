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
    private readonly ISettingsRepository _settingsRepository;
    private string _title = AppResources.PageTitleSettings;
    private Settings? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    public SettingsViewModel(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>
    /// Gets the command that loads the application settings.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

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

    private async Task LoadAsync()
    {
        Settings = await _settingsRepository.GetAsync();
    }
}
