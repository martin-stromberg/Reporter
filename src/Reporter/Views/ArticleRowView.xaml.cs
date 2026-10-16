// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Windows.Input;
using Reporter.Core.Models;

namespace Reporter.Views;

/// <summary>
/// Editorial list row of the start page: source, relative time, serif headline, teaser,
/// thumbnail and quick actions. Swipe right saves, swipe left marks the article as read.
/// </summary>
public partial class ArticleRowView : ContentView
{
    /// <summary>
    /// Identifies the <see cref="ToggleSavedCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty ToggleSavedCommandProperty = BindableProperty.Create(
        nameof(ToggleSavedCommand),
        typeof(ICommand),
        typeof(ArticleRowView));

    /// <summary>
    /// Identifies the <see cref="MarkReadCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty MarkReadCommandProperty = BindableProperty.Create(
        nameof(MarkReadCommand),
        typeof(ICommand),
        typeof(ArticleRowView));

    /// <summary>
    /// Identifies the <see cref="IsOnline"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty IsOnlineProperty = BindableProperty.Create(
        nameof(IsOnline),
        typeof(bool),
        typeof(ArticleRowView),
        (object)true);

    /// <summary>
    /// Identifies the <see cref="IsSavedList"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty IsSavedListProperty = BindableProperty.Create(
        nameof(IsSavedList),
        typeof(bool),
        typeof(ArticleRowView),
        (object)false);

    private bool _isBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleRowView"/> class.
    /// </summary>
    public ArticleRowView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the command that toggles the saved-for-later state.
    /// </summary>
    public ICommand? ToggleSavedCommand
    {
        get => (ICommand?)GetValue(ToggleSavedCommandProperty);
        set => SetValue(ToggleSavedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that marks the article as read.
    /// </summary>
    public ICommand? MarkReadCommand
    {
        get => (ICommand?)GetValue(MarkReadCommandProperty);
        set => SetValue(MarkReadCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the device is online; remote images are hidden offline.
    /// </summary>
    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the row is shown in the saved-for-later list:
    /// swipe right and the bookmark remove the article from the list, marking as read keeps it.
    /// </summary>
    public bool IsSavedList
    {
        get => (bool)GetValue(IsSavedListProperty);
        set => SetValue(IsSavedListProperty, value);
    }

    /// <inheritdoc />
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // CollectionView recycelt Zeilen: Reste einer Ausblend-Animation zurücksetzen.
        this.AbortAnimation("RowDismiss");
        Opacity = 1;
        TranslationX = 0;
        Surface.Scale = 1;
        _isBusy = false;
    }

    private async void OnOpenTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not ItemListItem item)
        {
            return;
        }

        await Surface.ScaleToAsync(0.98, 70, Easing.CubicOut);
        await Surface.ScaleToAsync(1, 140, Easing.CubicOut);
        await ArticleNavigation.OpenAsync(item);
    }

    private async void OnBookmarkTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not ItemListItem item)
        {
            return;
        }

        if (IsSavedList)
        {
            await DismissAsync(1, ToggleSavedCommand);
            return;
        }

        Haptics.Tick();
        ExecuteIfPossible(ToggleSavedCommand, item);
        await BookmarkIcon.ScaleToAsync(1.1, 90, Easing.CubicOut);
        await BookmarkIcon.ScaleToAsync(0.8, 160, Easing.SpringOut);
    }

    private async void OnSwipeSaveInvoked(object? sender, EventArgs e)
    {
        if (IsSavedList)
        {
            await DismissAsync(1, ToggleSavedCommand);
        }
        else if (BindingContext is ItemListItem item)
        {
            Haptics.Confirm();
            ExecuteIfPossible(ToggleSavedCommand, item);
        }
    }

    private async void OnSwipeDoneInvoked(object? sender, EventArgs e)
    {
        await MarkReadAsync();
    }

    private async void OnMarkReadTapped(object? sender, TappedEventArgs e)
    {
        await MarkReadAsync();
    }

    private async Task MarkReadAsync()
    {
        // In der Merkliste bleibt ein gelesener Artikel stehen und wird nur als gelesen dargestellt.
        if (IsSavedList)
        {
            if (BindingContext is ItemListItem item)
            {
                Haptics.Tick();
                ExecuteIfPossible(MarkReadCommand, item);
            }

            return;
        }

        await DismissAsync(-1, MarkReadCommand);
    }

    private async Task DismissAsync(int direction, ICommand? command)
    {
        if (_isBusy || BindingContext is not ItemListItem item)
        {
            return;
        }

        _isBusy = true;
        Haptics.Confirm();
        await Task.WhenAll(
            this.FadeToAsync(0, 180, Easing.CubicIn),
            this.TranslateToAsync(direction * 48, 0, 180, Easing.CubicIn));
        ExecuteIfPossible(command, item);

        // Fallback: bleibt der Artikel (z. B. nach einem Fehler) in der Liste, Zeile wieder einblenden.
        await Task.Delay(700);
        if (ReferenceEquals(BindingContext, item))
        {
            _isBusy = false;
            await Task.WhenAll(this.FadeToAsync(1, 160), this.TranslateToAsync(0, 0, 160, Easing.CubicOut));
        }
    }

    private static void ExecuteIfPossible(ICommand? command, ItemListItem item)
    {
        if (command?.CanExecute(item) == true)
        {
            command.Execute(item);
        }
    }
}
