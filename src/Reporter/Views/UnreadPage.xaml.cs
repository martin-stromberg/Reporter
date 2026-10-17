// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Start page with unread articles: editorial header, animated category tabs and either a
/// scrolling list or swipeable cards (configurable in the settings and via the header toggle).
/// </summary>
public partial class UnreadPage : ContentPage
{
    private const string RefreshSpin = "RefreshSpin";

    private readonly UnreadViewModel _viewModel;
    private bool _hasAppeared;
    private const double SwipeThreshold = 90;
    private const double TapSlop = 8;
    private const double BackCardScale = 0.93;
    private const double BackCardOffset = 26;
    private const double BackCardOpacity = 0.55;

    private bool _isAnimatingCard;
    private double _panX;
    private double _panY;
    private Point? _pointerOrigin;
    private bool _isPointerCaptured;
    private bool _filterChangePending;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public UnreadPage(UnreadViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        RegisterCardDragGesture();
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateRefreshSpin();

        if (!_hasAppeared)
        {
            _hasAppeared = true;
            _ = PlayEntranceAsync();
        }

        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UnreadPage.OnAppearing failed: {ex}");
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        this.AbortAnimation(RefreshSpin);
        RefreshIcon.Rotation = 0;
    }

    private async Task PlayEntranceAsync()
    {
        LayoutRoot.Opacity = 0;
        LayoutRoot.TranslationY = 12;
        await Task.WhenAll(
            LayoutRoot.FadeToAsync(1, 380, Easing.CubicOut),
            LayoutRoot.TranslateToAsync(0, 0, 420, Easing.CubicOut));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UnreadViewModel.IsSyncing):
                UpdateRefreshSpin();
                break;
            case nameof(UnreadViewModel.IsCardLayout):
                _ = RevealAsync(_viewModel.IsCardLayout ? CardsHost : ListHost);
                break;
            case nameof(UnreadViewModel.SelectedCategory):
                _filterChangePending = true;
                break;
            case nameof(UnreadViewModel.Articles) when _filterChangePending:
                _filterChangePending = false;
                _ = RevealAsync(_viewModel.IsCardLayout ? CardsHost : ListHost);
                break;
        }
    }

    private static async Task RevealAsync(VisualElement host)
    {
        host.Opacity = 0;
        host.TranslationY = 10;
        await Task.WhenAll(
            host.FadeToAsync(1, 260, Easing.CubicOut),
            host.TranslateToAsync(0, 0, 300, Easing.CubicOut));
    }

    private void UpdateRefreshSpin()
    {
        this.AbortAnimation(RefreshSpin);
        if (!_viewModel.IsSyncing)
        {
            _ = RefreshIcon.RotateToAsync(0, 0);
            return;
        }

        new Animation(v => RefreshIcon.Rotation = v, 0, 360)
            .Commit(this, RefreshSpin, length: 900, easing: Easing.Linear, repeat: () => _viewModel.IsSyncing);
    }

    private void OnLayoutToggleTapped(object? sender, TappedEventArgs e)
    {
        Haptics.Tick();
        _viewModel.ToggleLayoutCommand.Execute(null);
    }

    private async void OnMarkAllReadTapped(object? sender, TappedEventArgs e)
    {
        if (!_viewModel.HasArticles)
        {
            return;
        }

        var choice = await DisplayActionSheetAsync(
            _viewModel.UnreadBadgeText,
            AppResources.ButtonCancel,
            AppResources.ButtonMarkAllRead);

        if (choice == AppResources.ButtonMarkAllRead)
        {
            Haptics.Confirm();
            await _viewModel.MarkAllReadCommand.ExecuteAsync(null);
        }
    }

    private async void OnCardSaveTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.CurrentCard is not { } card)
        {
            return;
        }

        Haptics.Tick();
        _ = _viewModel.ToggleSavedCommand.ExecuteAsync(card);
        await PopAsync(CardSaveButton);
    }

    private async void OnCardOpenTapped(object? sender, TappedEventArgs e)
    {
        await PopAsync(CardOpenButton);
        await ArticleNavigation.OpenAsync(_viewModel.CurrentCard);
    }

    private async void OnCardDoneTapped(object? sender, TappedEventArgs e)
    {
        _ = PopAsync(CardDoneButton);
        await DismissCardAsync();
    }

    private async void OnCardNextTapped(object? sender, TappedEventArgs e)
    {
        await AdvanceAsync(forward: true);
    }

    private async void OnCardPreviousTapped(object? sender, TappedEventArgs e)
    {
        await AdvanceAsync(forward: false);
    }

    private void OnCardPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                BeginDrag();
                break;
            case GestureStatus.Running:
                UpdateDrag(e.TotalX, e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                EndDrag();
                break;
        }
    }

    private void RegisterCardDragGesture()
    {
#if WINDOWS
        // WinUI meldet die Pan-Manipulation nur für Touch/Stift und das Tap-Overlay der Karte
        // fängt Pointer-Events ab; daher native Pointer-Events inkl. bereits behandelter abonnieren.
        FrontCard.HandlerChanged += (_, _) =>
        {
            if (FrontCard.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element)
            {
                element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnNativeCardPointerPressed), true);
                element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnNativeCardPointerMoved), true);
                element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnNativeCardPointerReleased), true);
                element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerCanceledEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(OnNativeCardPointerReleased), true);
            }
        };
#else
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnCardPanUpdated;
        FrontCard.GestureRecognizers.Add(pan);
#endif
    }

#if WINDOWS
    private Point? GetStagePosition(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (CardStage.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement stage)
        {
            return null;
        }

        var position = e.GetCurrentPoint(stage).Position;
        return new Point(position.X, position.Y);
    }

    private void OnNativeCardPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        FrontCard.SuppressTap = false;
        _pointerOrigin = GetStagePosition(e);
        if (_pointerOrigin is not null)
        {
            BeginDrag();
        }
    }

    private void OnNativeCardPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_pointerOrigin is { } origin && GetStagePosition(e) is { } point)
        {
            // Ab echtem Drag den Pointer festhalten, damit Moves und Loslassen auch außerhalb der Karte ankommen.
            if (!_isPointerCaptured && sender is Microsoft.UI.Xaml.UIElement element
                && (Math.Abs(point.X - origin.X) > TapSlop || Math.Abs(point.Y - origin.Y) > TapSlop))
            {
                _isPointerCaptured = element.CapturePointer(e.Pointer);
            }

            UpdateDrag(point.X - origin.X, point.Y - origin.Y);
        }
    }

    private void OnNativeCardPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_pointerOrigin is not { } origin)
        {
            return;
        }

        _pointerOrigin = null;
        if (_isPointerCaptured && sender is Microsoft.UI.Xaml.UIElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
        }

        _isPointerCaptured = false;
        if (GetStagePosition(e) is { } point)
        {
            UpdateDrag(point.X - origin.X, point.Y - origin.Y);
        }

        // Nach einem echten Wisch darf das anschließende Tap-Ereignis den Artikel nicht öffnen.
        FrontCard.SuppressTap = Math.Abs(_panX) > TapSlop || Math.Abs(_panY) > TapSlop;
        EndDrag();
    }
#endif

    private void BeginDrag()
    {
        _panX = 0;
        _panY = 0;
    }

    private void UpdateDrag(double totalX, double totalY)
    {
        if (_isAnimatingCard)
        {
            return;
        }

        _panX = totalX;

        // Nach unten nur mit Gummiband-Widerstand, nach oben frei (Geste „gelesen").
        _panY = totalY > 0 ? totalY * 0.2 : totalY;
        FrontCard.TranslationX = _panX;
        FrontCard.TranslationY = _panY;
        FrontCard.Rotation = Math.Clamp(_panX / 24, -9, 9);

        var towardsNext = Math.Max(-_panX, -_panY);
        SetBackCardProgress(Math.Clamp(towardsNext / SwipeThreshold, 0, 1));
    }

    private void EndDrag()
    {
        if (!_isAnimatingCard && (_panX != 0 || _panY != 0))
        {
            _ = ResolvePanAsync();
        }
    }

    private async Task ResolvePanAsync()
    {
        if (_panY < -SwipeThreshold && Math.Abs(_panY) > Math.Abs(_panX))
        {
            await DismissCardAsync();
        }
        else if (_panX < -SwipeThreshold && _viewModel.CanGoToNextCard)
        {
            await AdvanceAsync(forward: true);
        }
        else if (_panX > SwipeThreshold && _viewModel.CanGoToPreviousCard)
        {
            await AdvanceAsync(forward: false);
        }
        else
        {
            await SpringBackAsync();
        }

        _panX = 0;
        _panY = 0;
    }

    private async Task SpringBackAsync()
    {
        _isAnimatingCard = true;
        try
        {
            await Task.WhenAll(
                FrontCard.TranslateToAsync(0, 0, 320, Easing.SpringOut),
                FrontCard.RotateToAsync(0, 260, Easing.CubicOut),
                AnimateBackCardAsync(0, 220));
        }
        finally
        {
            _isAnimatingCard = false;
        }
    }

    private async Task AdvanceAsync(bool forward)
    {
        if (_isAnimatingCard || !(forward ? _viewModel.CanGoToNextCard : _viewModel.CanGoToPreviousCard))
        {
            return;
        }

        _isAnimatingCard = true;
        try
        {
            Haptics.Tick();
            var direction = forward ? -1 : 1;
            var distance = Math.Max(CardStage.Width, 320) * 1.3;
            await Task.WhenAll(
                FrontCard.TranslateToAsync(direction * distance, FrontCard.TranslationY, 230, Easing.CubicIn),
                FrontCard.RotateToAsync(direction * 12, 230, Easing.CubicIn),
                forward ? AnimateBackCardAsync(1, 230) : Task.CompletedTask);

            if (forward)
            {
                _viewModel.NextCardCommand.Execute(null);
                ResetFrontCard();
                ResetBackCard();
                await BackCard.FadeToAsync(BackCardOpacity, 200, Easing.CubicOut);
            }
            else
            {
                _viewModel.PreviousCardCommand.Execute(null);
                ResetFrontCard();
                FrontCard.TranslationX = -60;
                FrontCard.Opacity = 0;
                await Task.WhenAll(
                    FrontCard.TranslateToAsync(0, 0, 280, Easing.CubicOut),
                    FrontCard.FadeToAsync(1, 220, Easing.CubicOut));
            }
        }
        finally
        {
            _isAnimatingCard = false;
        }
    }

    private async Task DismissCardAsync()
    {
        if (_isAnimatingCard || _viewModel.CurrentCard is not { } card)
        {
            return;
        }

        _isAnimatingCard = true;
        try
        {
            Haptics.Confirm();
            await Task.WhenAll(
                FrontCard.TranslateToAsync(FrontCard.TranslationX, -Math.Max(CardStage.Height, 400), 240, Easing.CubicIn),
                FrontCard.FadeToAsync(0, 240, Easing.CubicIn),
                AnimateBackCardAsync(1, 240));

            await _viewModel.MarkReadCommand.ExecuteAsync(card);

            ResetFrontCard();
            ResetBackCard();
            await BackCard.FadeToAsync(BackCardOpacity, 200, Easing.CubicOut);
        }
        finally
        {
            ResetFrontCard();
            _isAnimatingCard = false;
        }
    }

    private void SetBackCardProgress(double progress)
    {
        BackCard.Scale = BackCardScale + ((1 - BackCardScale) * progress);
        BackCard.TranslationY = BackCardOffset * (1 - progress);
        BackCard.Opacity = BackCardOpacity + ((1 - BackCardOpacity) * progress);
    }

    private Task AnimateBackCardAsync(double targetProgress, uint length)
    {
        var start = (BackCard.Scale - BackCardScale) / (1 - BackCardScale);
        var tcs = new TaskCompletionSource();
        new Animation(v => SetBackCardProgress(v), start, targetProgress)
            .Commit(BackCard, "BackCardProgress", length: length, easing: Easing.CubicOut, finished: (_, _) => tcs.TrySetResult());
        return tcs.Task;
    }

    private void ResetFrontCard()
    {
        FrontCard.TranslationX = 0;
        FrontCard.TranslationY = 0;
        FrontCard.Rotation = 0;
        FrontCard.Opacity = 1;
    }

    private void ResetBackCard()
    {
        SetBackCardProgress(0);
        BackCard.Opacity = 0;
    }

    private static async Task PopAsync(VisualElement element)
    {
        await element.ScaleToAsync(0.9, 70, Easing.CubicOut);
        await element.ScaleToAsync(1, 200, Easing.SpringOut);
    }
}
