// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Detail page for a single feed: shows the paged article list and hosts the
/// feed-level actions (refresh, rename, category, edit, sync message, delete).
/// </summary>
public partial class FeedDetailPage : ContentPage, IQueryAttributable
{
    private readonly FeedDetailViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedDetailPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public FeedDetailPage(FeedDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.NavigateBackAsync = () => Shell.Current.GoToAsync("..");
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("feedId", out var value))
        {
            _viewModel.ErrorMessage = AppResources.ErrorLoadFailed;
            return;
        }

        var feedIdString = value?.ToString();
        if (string.IsNullOrWhiteSpace(feedIdString) || !Guid.TryParse(feedIdString, out var feedId))
        {
            _viewModel.ErrorMessage = AppResources.ErrorLoadFailed;
            return;
        }

        _ = LoadAsync(feedId);
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.AttachConnectivity();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.DetachConnectivity();
        base.OnDisappearing();
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ShowEditForm)
        {
            _viewModel.CloseEditFormCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async Task LoadAsync(Guid feedId)
    {
        try
        {
            await _viewModel.LoadAsync(feedId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FeedDetailPage.LoadAsync failed: {ex}");
        }
    }

    /// <summary>
    /// Shows the action sheet for the current feed and routes the selected
    /// action to the view model or page methods.
    /// </summary>
    /// <param name="sender">The button that was clicked.</param>
    /// <param name="e">The event args.</param>
    private async void OnFeedActionsClicked(object? sender, EventArgs e)
    {
        try
        {
            var feed = _viewModel.Feed;
            if (feed is null)
            {
                return;
            }

            var actions = new List<string>
            {
                AppResources.ButtonRefresh,
                AppResources.ButtonRename,
                AppResources.ButtonChangeCategory,
                AppResources.ButtonEdit,
            };

            if (feed.HealthStatus is FeedHealth.Error or FeedHealth.Warning)
            {
                actions.Add(AppResources.ButtonShowMessage);
            }

            actions.Add(AppResources.ButtonDelete);

            var action = await DisplayActionSheetAsync(
                AppResources.ActionSheetTitleFeed,
                AppResources.ButtonCancel,
                null,
                [.. actions]);

            if (action == AppResources.ButtonRefresh)
            {
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }
            else if (action == AppResources.ButtonRename)
            {
                await RenameFeedAsync(feed);
            }
            else if (action == AppResources.ButtonChangeCategory)
            {
                await ChangeCategoryAsync(feed);
            }
            else if (action == AppResources.ButtonEdit)
            {
                _viewModel.EditCommand.Execute(null);
            }
            else if (action == AppResources.ButtonShowMessage)
            {
                await ShowFeedMessageAsync(feed);
            }
            else if (action == AppResources.ButtonDelete)
            {
                await ConfirmDeleteFeedAsync();
            }
        }
        catch (Exception ex)
        {
            // The handler is async void: a repository failure must not escape
            // as an unhandled exception on the UI context — it is logged and
            // surfaced on the page's error label instead.
            Debug.WriteLine($"OnFeedActionsClicked failed: {ex}");
            _viewModel.ErrorMessage = AppResources.ErrorActionFailed;
        }
    }

    /// <summary>
    /// Asks for a new display title and renames the feed when confirmed.
    /// </summary>
    /// <param name="feed">The feed to rename.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RenameFeedAsync(FeedListItem feed)
    {
        var newTitle = await DisplayPromptAsync(
            AppResources.PromptRenameFeedTitle,
            AppResources.PromptRenameFeedMessage,
            AppResources.ButtonOk,
            AppResources.ButtonCancel,
            initialValue: feed.Title);

        if (newTitle is not null)
        {
            await _viewModel.RenameFeedAsync(feed, newTitle);
        }
    }

    /// <summary>
    /// Shows the category picker and applies the chosen category to the feed.
    /// </summary>
    /// <param name="feed">The feed to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ChangeCategoryAsync(FeedListItem feed)
    {
        // A cancelled sheet returns the cancel text, so a category named exactly
        // like the cancel button could not be told apart from a cancellation —
        // it is excluded from the options.
        var categories = _viewModel.Categories
            .Where(c => !string.Equals(c.Name, AppResources.ButtonCancel, StringComparison.Ordinal))
            .ToList();

        // The action sheet returns the tapped button's text, not its position.
        // Categories cannot get duplicate names through the app, but a database
        // edited elsewhere may contain them; the labels are made collision-free
        // unique so the returned text maps to exactly one entry.
        var options = FeedDetailViewModel.MakeUniqueOptionLabels(
            categories.Select(c => c.Name).ToList());

        var selectedName = await DisplayActionSheetAsync(
            AppResources.LabelFeedCategory,
            AppResources.ButtonCancel,
            null,
            options.ToArray());

        var index = options.IndexOf(selectedName);
        if (index >= 0)
        {
            await _viewModel.ChangeFeedCategoryAsync(feed, categories[index]);
        }
    }

    /// <summary>
    /// Asks for confirmation and deletes the feed when confirmed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ConfirmDeleteFeedAsync()
    {
        var confirmed = await DisplayAlertAsync(
            AppResources.ConfirmDeleteFeedTitle,
            AppResources.ConfirmDeleteFeedMessage,
            AppResources.ButtonYes,
            AppResources.ButtonNo);

        if (confirmed)
        {
            await _viewModel.DeleteFeedAsync();
        }
    }

    /// <summary>
    /// Shows the last sync message of the feed — error or warning alike: a
    /// localized category text plus the stored technical message when present.
    /// </summary>
    /// <param name="feed">The feed whose sync message is shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ShowFeedMessageAsync(FeedListItem feed)
    {
        await DisplayAlertAsync(
            AppResources.FeedMessageDetailsTitle,
            _viewModel.GetFeedMessage(feed),
            AppResources.ButtonOk);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FeedDetailViewModel.ShowEditForm) && _viewModel.ShowEditForm)
        {
            Dispatcher.Dispatch(() => EditUrlEntry.Focus());
        }
    }
}
