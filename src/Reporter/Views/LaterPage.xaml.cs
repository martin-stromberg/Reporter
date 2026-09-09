namespace Reporter.Views;

public partial class LaterPage : ContentPage
{
    public LaterPage(ViewModels.LaterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
