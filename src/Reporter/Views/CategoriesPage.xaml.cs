using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for managing feed categories.
/// </summary>
public partial class CategoriesPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CategoriesViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
