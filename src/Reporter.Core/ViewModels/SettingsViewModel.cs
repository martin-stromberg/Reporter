using Reporter.Core.Interfaces;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the settings page.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsRepository _settingsRepository;
    private string _title = AppResources.PageTitleSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    public SettingsViewModel(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
