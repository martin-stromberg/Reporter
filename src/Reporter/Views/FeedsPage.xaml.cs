namespace Reporter.Views;

/// <summary>
/// Page for displaying feeds.
/// </summary>
public partial class FeedsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public FeedsPage(ViewModels.FeedsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
