// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
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
        viewModel.ConfirmDirectAddAsync = ConfirmDirectAddAsync;
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

    /// <summary>
    /// Asks the user whether the tapped search result should be subscribed to
    /// and routes a confirmation to the view model.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">Event args containing the tapped result as <see cref="TappedEventArgs.Parameter"/>.</param>
    private async void OnSearchResultTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not FeedSearchResult result || BindingContext is not FeedsViewModel viewModel)
        {
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(result.Title) ? result.FeedUrl : result.Title;
        var confirmed = await DisplayAlertAsync(
            AppResources.ConfirmSubscribeFeedTitle,
            string.Format(CultureInfo.CurrentCulture, AppResources.ConfirmSubscribeFeedMessage, displayName),
            AppResources.ButtonYes,
            AppResources.ButtonNo);

        if (confirmed)
        {
            await viewModel.SubscribeResultCommand.ExecuteAsync(result);
        }
    }

    /// <summary>
    /// Shows the fallback confirmation that offers adding the entered URL directly
    /// when the feed search found nothing or is unavailable.
    /// </summary>
    /// <param name="url">The URL the user entered.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> when the user wants to add the URL directly.</returns>
    private Task<bool> ConfirmDirectAddAsync(string url)
    {
        return DisplayAlertAsync(
            AppResources.FeedSearchNoResultsTitle,
            string.Format(CultureInfo.CurrentCulture, AppResources.FeedSearchNoResultsAddUrl, url),
            AppResources.ButtonYes,
            AppResources.ButtonNo);
    }
}
