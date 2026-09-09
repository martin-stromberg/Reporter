using Microsoft.Extensions.DependencyInjection;

namespace Reporter.Views;

public partial class EinstellungenPage : ContentPage
{
    public EinstellungenPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<ViewModels.EinstellungenViewModel>();
    }
}
