using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

public partial class LaterViewModel : BaseViewModel
{
    private string _title = AppResources.PageTitleLater;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
