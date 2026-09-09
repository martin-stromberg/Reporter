using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

/// <summary>
/// View model for the settings page.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private string _title = AppResources.PageTitleSettings;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
