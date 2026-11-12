// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Reporter.Core.Models;

namespace Reporter.Views;

/// <summary>
/// Horizontally scrolling category filter tabs with a pill indicator that glides
/// to the selected tab. Selection state is driven by <see cref="CategoryFilterItem.IsSelected"/>.
/// </summary>
public partial class CategoryFilterTabs : ContentView
{
    /// <summary>
    /// Identifies the <see cref="ItemsSource"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(CategoryFilterTabs),
        propertyChanged: (BindableProperty.BindingPropertyChangedDelegate)OnItemsSourceChanged);

    /// <summary>
    /// Identifies the <see cref="SelectCommand"/> bindable property.
    /// </summary>
    /// <returns>The bindable property.</returns>
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand),
        typeof(ICommand),
        typeof(CategoryFilterTabs));

    private const string IndicatorAnimation = "FilterIndicator";
    private static readonly Easing EmphasizedDecelerate = new(t => 1 - Math.Pow(1 - t, 4));

    private readonly List<CategoryFilterItem> _observedItems = [];
    private INotifyCollectionChanged? _observedCollection;
    private bool _hasPlacedIndicator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryFilterTabs"/> class.
    /// </summary>
    public CategoryFilterTabs()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the filter items shown as tabs.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the command executed with the tapped <see cref="CategoryFilterItem"/>.
    /// </summary>
    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var tabs = (CategoryFilterTabs)bindable;
        if (tabs._observedCollection is not null)
        {
            tabs._observedCollection.CollectionChanged -= tabs.OnItemsCollectionChanged;
        }

        tabs._observedCollection = newValue as INotifyCollectionChanged;
        if (tabs._observedCollection is not null)
        {
            tabs._observedCollection.CollectionChanged += tabs.OnItemsCollectionChanged;
        }

        tabs.ObserveItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ObserveItems();
    }

    private void ObserveItems()
    {
        foreach (var item in _observedItems)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        _observedItems.Clear();
        if (ItemsSource is not null)
        {
            _observedItems.AddRange(ItemsSource.OfType<CategoryFilterItem>());
        }

        foreach (var item in _observedItems)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        Dispatcher.Dispatch(() => UpdateIndicator(scrollIntoView: false));
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryFilterItem.IsSelected) && sender is CategoryFilterItem { IsSelected: true })
        {
            Dispatcher.Dispatch(() => UpdateIndicator(scrollIntoView: true));
        }
    }

    private void OnTabsHostSizeChanged(object? sender, EventArgs e)
    {
        UpdateIndicator(scrollIntoView: false);
    }

    private void OnTabAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is View tab)
        {
            tab.SizeChanged += OnTabsHostSizeChanged;
        }
    }

    private async void OnTabTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not View tab || tab.BindingContext is not CategoryFilterItem item)
        {
            return;
        }

        Haptics.Tick();
        if (!item.IsSelected && SelectCommand?.CanExecute(item) == true)
        {
            SelectCommand.Execute(item);
        }

        await tab.ScaleToAsync(0.94, 70, Easing.CubicOut);
        await tab.ScaleToAsync(1, 160, Easing.SpringOut);
    }

    private void UpdateIndicator(bool scrollIntoView)
    {
        var selected = TabsHost.Children
            .OfType<View>()
            .FirstOrDefault(v => v.BindingContext is CategoryFilterItem { IsSelected: true });

        if (selected is null || selected.Width <= 0)
        {
            return;
        }

        var targetX = selected.Frame.X;
        var targetWidth = selected.Width;

        this.AbortAnimation(IndicatorAnimation);
        if (!_hasPlacedIndicator)
        {
            _hasPlacedIndicator = true;
            Indicator.TranslationX = targetX;
            Indicator.WidthRequest = targetWidth;
            _ = Indicator.FadeToAsync(1, 200, Easing.CubicOut);
            return;
        }

        var startX = Indicator.TranslationX;
        var startWidth = Indicator.WidthRequest;
        if (Math.Abs(startX - targetX) < 0.5 && Math.Abs(startWidth - targetWidth) < 0.5)
        {
            return;
        }

        new Animation(t =>
        {
            Indicator.TranslationX = startX + ((targetX - startX) * t);
            Indicator.WidthRequest = startWidth + ((targetWidth - startWidth) * t);
        }).Commit(this, IndicatorAnimation, length: 320, easing: EmphasizedDecelerate);

        if (scrollIntoView)
        {
            _ = Scroller.ScrollToAsync(selected, ScrollToPosition.Center, true);
        }
    }
}
