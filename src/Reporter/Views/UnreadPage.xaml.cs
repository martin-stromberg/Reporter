// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.Maui.Controls;
using Reporter.Core.Resources.Strings;
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

    /// <summary>
    /// Opens an action sheet to select a category filter.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">The event args.</param>
    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not UnreadViewModel viewModel || viewModel.Categories.Count == 0)
        {
            return;
        }

        var options = viewModel.Categories.Select(c => c.Name).ToArray();
        var action = await DisplayActionSheetAsync(
            AppResources.ActionSheetTitleCategory,
            AppResources.ButtonCancel,
            null,
            options);

        var selected = viewModel.Categories.FirstOrDefault(c => c.Name == action);
        if (selected is not null)
        {
            try
            {
                await viewModel.SelectCategoryCommand.ExecuteAsync(selected);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnFilterClicked failed: {ex}");
            }
        }
    }
}
