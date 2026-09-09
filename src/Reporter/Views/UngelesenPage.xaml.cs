using Microsoft.Extensions.DependencyInjection;

namespace Reporter.Views;

public partial class UngelesenPage : ContentPage
{
    public UngelesenPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<ViewModels.UngelesenViewModel>();
    }
}
