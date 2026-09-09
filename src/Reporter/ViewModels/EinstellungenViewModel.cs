using CommunityToolkit.Mvvm.ComponentModel;

namespace Reporter.ViewModels;

public partial class EinstellungenViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _title = "Einstellungen";
}
