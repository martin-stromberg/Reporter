using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for displaying articles saved for later.
/// </summary>
public partial class LaterPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LaterPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public LaterPage(LaterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
