using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
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

    /// <summary>
    /// Shows an action sheet for the tapped category and routes the selected
    /// action to the view model.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">Event args containing the tapped category as <see cref="TappedEventArgs.Parameter"/>.</param>
    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not CategoryWithCount category || BindingContext is not CategoriesViewModel viewModel)
        {
            return;
        }

        var action = await DisplayActionSheetAsync(
            AppResources.ActionSheetTitleCategory,
            AppResources.ButtonCancel,
            null,
            AppResources.ButtonEdit,
            AppResources.ButtonDelete);

        if (action == AppResources.ButtonEdit)
        {
            await viewModel.EditCommand.ExecuteAsync(category);
            CategoryNameEntry?.Focus();
        }
        else if (action == AppResources.ButtonDelete)
        {
            var confirmed = await DisplayAlertAsync(
                AppResources.ConfirmDeleteFeedTitle,
                $"{category.Name} ({category.FeedCount} {AppResources.LabelCategoryFeedCount})",
                AppResources.ButtonYes,
                AppResources.ButtonNo);

            if (confirmed)
            {
                await viewModel.DeleteCommand.ExecuteAsync(category);
            }
        }
    }
}
