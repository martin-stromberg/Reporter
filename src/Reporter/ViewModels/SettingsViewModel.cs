using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private string _title = AppResources.PageTitleSettings;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
