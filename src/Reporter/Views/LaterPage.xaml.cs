// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
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

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LaterViewModel viewModel)
        {
            try
            {
                await viewModel.LoadCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LaterPage.OnAppearing failed: {ex}");
            }
        }
    }
}
