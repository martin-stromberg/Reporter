// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Views;

/// <summary>
/// Full-size article card of the card (triage) layout: large media area or typographic
/// cover, display headline and teaser. Tapping opens the article.
/// </summary>
public partial class ArticleHeroCardView : ContentView
{
    /// <summary>
    /// Identifies the <see cref="IsOnline"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty IsOnlineProperty = BindableProperty.Create(
        nameof(IsOnline),
        typeof(bool),
        typeof(ArticleHeroCardView),
        (object)true);

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleHeroCardView"/> class.
    /// </summary>
    public ArticleHeroCardView()
    {
        InitializeComponent();
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
    /// Gets or sets a value indicating whether the next tap is ignored because it ended a swipe.
    /// </summary>
    public bool SuppressTap { get; set; }

    private void OnPanelSizeChanged(object? sender, EventArgs e)
    {
        // Bildbereich endet exakt am Textpanel – unabhängig davon, wie die CarouselView misst.
        MediaHost.Margin = new Thickness(0, 0, 0, Math.Max(0, Panel.Height));
    }

    private async void OnOpenTapped(object? sender, TappedEventArgs e)
    {
        if (SuppressTap)
        {
            SuppressTap = false;
            return;
        }

        if (BindingContext is not ItemListItem item)
        {
            return;
        }

        await Surface.ScaleToAsync(0.985, 70, Easing.CubicOut);
        await Surface.ScaleToAsync(1, 160, Easing.CubicOut);
        await ArticleNavigation.OpenAsync(item);
    }
}
