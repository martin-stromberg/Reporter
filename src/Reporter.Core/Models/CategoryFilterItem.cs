using CommunityToolkit.Mvvm.ComponentModel;

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
    private int _count;

    /// <summary>
    /// Gets or sets a value indicating whether this filter is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}
