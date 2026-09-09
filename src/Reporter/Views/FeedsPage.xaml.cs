using Microsoft.Extensions.DependencyInjection;

namespace Reporter.Views;

public partial class FeedsPage : ContentPage
{
    public FeedsPage()
    {
        InitializeComponent();
        BindingContext = App.Services?.GetRequiredService<ViewModels.FeedsViewModel>();
    }
}
