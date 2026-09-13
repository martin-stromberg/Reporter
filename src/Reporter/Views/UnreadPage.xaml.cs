// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.Maui.Controls;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for displaying unread articles with filter, pull-to-refresh and infinite scroll.
/// </summary>
public partial class UnreadPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public UnreadPage(UnreadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is UnreadViewModel viewModel)
        {
            try
            {
                await viewModel.LoadCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UnreadPage.OnAppearing failed: {ex}");
            }
        }
    }
}
