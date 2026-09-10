using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for managing feeds.
/// </summary>
public partial class FeedsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public FeedsPage(FeedsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is FeedsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    /// <summary>
    /// Shows an action sheet for the tapped feed and routes the selected action
    /// to the view model.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">Event args containing the tapped feed as <see cref="TappedEventArgs.Parameter"/>.</param>
    private async void OnFeedTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not FeedListItem feed || BindingContext is not FeedsViewModel viewModel)
        {
            return;
        }

        var action = await DisplayActionSheetAsync(
            AppResources.ActionSheetTitleFeed,
            AppResources.ButtonCancel,
            null,
            AppResources.ButtonRefresh,
            AppResources.ButtonEdit,
            AppResources.ButtonDelete);

        if (action == AppResources.ButtonRefresh)
        {
            await viewModel.RefreshCommand.ExecuteAsync(feed);
        }
        else if (action == AppResources.ButtonEdit)
        {
            await viewModel.EditCommand.ExecuteAsync(feed);
        }
        else if (action == AppResources.ButtonDelete)
        {
            var confirmed = await DisplayAlertAsync(
                AppResources.ConfirmDeleteFeedTitle,
                AppResources.ConfirmDeleteFeedMessage,
                AppResources.ButtonYes,
                AppResources.ButtonNo);

            if (confirmed)
            {
                await viewModel.DeleteCommand.ExecuteAsync(feed);
            }
        }
    }
}
