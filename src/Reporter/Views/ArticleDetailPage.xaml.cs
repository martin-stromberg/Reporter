// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.Maui.Controls;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for reading an article in a focused WebView with status and action controls.
/// </summary>
public partial class ArticleDetailPage : ContentPage, IQueryAttributable
{
    private readonly ArticleDetailViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleDetailPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public ArticleDetailPage(ArticleDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("itemId", out var value))
        {
            return;
        }

        var itemIdString = value?.ToString();
        if (string.IsNullOrWhiteSpace(itemIdString) || !Guid.TryParse(itemIdString, out var itemId))
        {
            return;
        }

        _ = LoadAsync(itemId);
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.AttachConnectivity();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.DetachConnectivity();
    }

    private async Task LoadAsync(Guid itemId)
    {
        try
        {
            await _viewModel.LoadAsync(itemId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ArticleDetailPage.LoadAsync failed: {ex}");
        }
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (_viewModel.IsOnline || !WebViewNavigationGuard.IsExternalUrl(e.Url))
        {
            return;
        }

        e.Cancel = true;
        _ = DisplayAlertAsync(AppResources.OfflineHint, AppResources.ArticleOfflineLinksDisabled, AppResources.ButtonOk);
    }
}
