using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

/// <summary>
/// View model for the later articles page.
/// </summary>
public partial class LaterViewModel : BaseViewModel
{
    private string _title = AppResources.PageTitleLater;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
