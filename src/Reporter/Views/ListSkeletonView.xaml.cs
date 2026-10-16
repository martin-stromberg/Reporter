// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Views;

/// <summary>
/// Pulsing placeholder rows shown while a list loads its first page. The pulse runs only
/// while the view is visible and attached.
/// </summary>
public partial class ListSkeletonView : ContentView
{
    private const string PulseAnimation = "SkeletonPulse";

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSkeletonView"/> class.
    /// </summary>
    public ListSkeletonView()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdatePulse();
        Unloaded += (_, _) => this.AbortAnimation(PulseAnimation);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsVisible))
        {
            UpdatePulse();
        }
    }

    private void UpdatePulse()
    {
        this.AbortAnimation(PulseAnimation);
        Rows.Opacity = 1;
        if (!IsVisible || Handler is null)
        {
            return;
        }

        new Animation
        {
            { 0, 0.5, new Animation(v => Rows.Opacity = v, 1, 0.45, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => Rows.Opacity = v, 0.45, 1, Easing.SinInOut) },
        }.Commit(this, PulseAnimation, length: 1400, repeat: () => IsVisible);
    }
}
