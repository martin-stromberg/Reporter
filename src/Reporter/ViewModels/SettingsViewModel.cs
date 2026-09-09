namespace Reporter.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private string _title = "Einstellungen";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
