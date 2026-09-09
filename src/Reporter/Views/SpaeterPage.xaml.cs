using Microsoft.Extensions.DependencyInjection;

namespace Reporter.Views;

public partial class SpaeterPage : ContentPage
{
    public SpaeterPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<ViewModels.SpaeterViewModel>();
    }
}
