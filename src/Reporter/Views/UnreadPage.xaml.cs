using Reporter.Core.Models;
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
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is UnreadViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
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
            await viewModel.SelectCategoryCommand.ExecuteAsync(selected);
        }
    }

    /// <summary>
    /// Shows an action sheet for the tapped article.
    /// </summary>
    /// <param name="sender">The view that received the tap.</param>
    /// <param name="e">Event args containing the tapped article as <see cref="TappedEventArgs.Parameter"/>.</param>
    private async void OnArticleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ItemListItem item || BindingContext is not UnreadViewModel viewModel)
        {
            return;
        }

        var action = await DisplayActionSheetAsync(
            AppResources.ActionSheetArticle,
            AppResources.ButtonCancel,
            null,
            AppResources.ButtonOpen,
            AppResources.ButtonBookmark,
            AppResources.ButtonMarkAsRead);

        if (action == AppResources.ButtonOpen)
        {
            if (!string.IsNullOrEmpty(item.Link))
            {
                await Browser.OpenAsync(item.Link, BrowserLaunchMode.SystemPreferred);
            }
        }
        else if (action == AppResources.ButtonBookmark)
        {
            await viewModel.ToggleSavedCommand.ExecuteAsync(item);
        }
        else if (action == AppResources.ButtonMarkAsRead)
        {
            await viewModel.MarkReadCommand.ExecuteAsync(item);
        }
    }
}
