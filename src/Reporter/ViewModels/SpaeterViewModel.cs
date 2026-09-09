using CommunityToolkit.Mvvm.ComponentModel;

namespace Reporter.ViewModels;

public partial class SpaeterViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _title = "Später";
}
