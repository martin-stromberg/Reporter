namespace Reporter.Views;

public partial class UnreadPage : ContentPage
{
    public UnreadPage(ViewModels.UnreadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
