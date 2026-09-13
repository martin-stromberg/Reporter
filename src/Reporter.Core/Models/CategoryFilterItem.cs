// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Models;

/// <summary>
/// Represents a category filter chip on the unread dashboard.
/// </summary>
public partial class CategoryFilterItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the optional category identifier. <c>null</c> represents "All".
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the filter chip.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of unread articles in this category.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibilityDescription))]
    private int _count;

    /// <summary>
    /// Gets or sets a value indicating whether this filter is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibilityDescription))]
    private bool _isSelected;

    /// <summary>
    /// Gets the localized screen-reader description of the chip, including the
    /// filter name, unread count, and selection state.
    /// </summary>
    public string AccessibilityDescription
    {
        get
        {
            var format = Count == 1
                ? AppResources.AccessibilityCategoryFilterSingular
                : AppResources.AccessibilityCategoryFilter;
            var state = IsSelected ? AppResources.AccessibilitySelected : AppResources.AccessibilityNotSelected;
            return string.Format(CultureInfo.CurrentCulture, format, Name, Count, state);
        }
    }
}
