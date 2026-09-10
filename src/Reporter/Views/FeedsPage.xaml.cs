using Reporter.Core.Models;
using Reporter.Resources.Strings;
using Reporter.ViewModels;

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
    /// Handles the delete button click by asking for confirmation and then deleting the feed.
    /// </summary>
    /// <param name="sender">The button that was clicked.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not FeedListItem feed)
        {
            return;
        }

        if (BindingContext is not FeedsViewModel viewModel)
        {
            return;
        }

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
