// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for displaying articles saved for later: editorial header, animated category tabs
/// and the saved list (swipe right removes, swipe left marks as read).
/// </summary>
public partial class LaterPage : ContentPage
{
    private readonly LaterViewModel _viewModel;
    private bool _hasAppeared;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public LaterPage(LaterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (!_hasAppeared)
        {
            _hasAppeared = true;
            LayoutRoot.Opacity = 0;
            LayoutRoot.TranslationY = 12;
            _ = Task.WhenAll(
                LayoutRoot.FadeToAsync(1, 380, Easing.CubicOut),
                LayoutRoot.TranslateToAsync(0, 0, 420, Easing.CubicOut));
        }

        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LaterPage.OnAppearing failed: {ex}");
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Beim Filterwechsel blendet die Liste sanft neu ein.
        if (e.PropertyName == nameof(LaterViewModel.SelectedCategory) && _viewModel.SelectCategoryCommand.IsRunning)
        {
            SavedCollection.Opacity = 0;
            SavedCollection.TranslationY = 10;
            _ = Task.WhenAll(
                SavedCollection.FadeToAsync(1, 260, Easing.CubicOut),
                SavedCollection.TranslateToAsync(0, 0, 300, Easing.CubicOut));
        }
    }
}
