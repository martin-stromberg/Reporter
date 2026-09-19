// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
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
    private readonly FeedsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public FeedsPage(FeedsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        viewModel.ConfirmDirectAddAsync = ConfirmDirectAddAsync;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // The view model is a singleton while the page is transient: subscribe
        // only while the page is shown and unsubscribe on leave, otherwise old
        // page instances would accumulate on the event and stay alive.
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.LoadCommand.Execute(null);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnDisappearing();
    }

    /// <summary>
    /// Navigates to the detail page of the tapped feed.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">Event args containing the tapped feed as <see cref="TappedEventArgs.Parameter"/>.</param>
    private async void OnFeedTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not FeedListItem feed)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync($"feeddetail?feedId={feed.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnFeedTapped navigation failed: {ex}");
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

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is FeedsViewModel viewModel && viewModel.ShowAddForm)
        {
            viewModel.CloseAddFormCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FeedsViewModel.ShowAddForm) && _viewModel.ShowAddForm)
        {
            Dispatcher.Dispatch(() => NewUrlEntry.Focus());
        }
    }
}
