// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for managing categories.
/// </summary>
public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryRepository _categoryRepository;
    private string _newCategoryName = string.Empty;
    private CategoryWithCount? _selectedCategory;
    private string _errorMessage = string.Empty;
    private ObservableCollection<CategoryWithCount> _categories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesViewModel"/> class.
    /// </summary>
    /// <param name="categoryRepository">The category repository.</param>
    public CategoriesViewModel(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        EditCommand = new AsyncRelayCommand<CategoryWithCount?>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<CategoryWithCount?>(DeleteAsync);
    }

    /// <summary>
    /// Gets the command that loads the categories.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that saves a new or existing category.
    /// </summary>
    public AsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// Gets the command that prepares an existing category for editing.
    /// </summary>
    public AsyncRelayCommand<CategoryWithCount?> EditCommand { get; }

    /// <summary>
    /// Gets the command that deletes a category.
    /// </summary>
    public AsyncRelayCommand<CategoryWithCount?> DeleteCommand { get; }

    /// <summary>
    /// Gets or sets the name for a new or edited category.
    /// </summary>
    public string NewCategoryName
    {
        get => _newCategoryName;
        set => SetProperty(ref _newCategoryName, value);
    }

    /// <summary>
    /// Gets or sets the category currently selected for editing.
    /// </summary>
    public CategoryWithCount? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                NewCategoryName = value?.Name ?? string.Empty;
                ErrorMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets the current error message.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an error message is present.
    /// </summary>
    public bool HasError => _errorMessage.Length > 0;

    /// <summary>
    /// Gets or sets the list of categories with their assigned feed counts.
    /// </summary>
    public ObservableCollection<CategoryWithCount> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        var categories = await _categoryRepository.GetAllWithFeedCountAsync();
        Categories = new ObservableCollection<CategoryWithCount>(categories);
    }

    private async Task SaveAsync()
    {
        var name = NewCategoryName.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = AppResources.ErrorCategoryNameEmpty;
            return;
        }

        if (Categories.Any(c =>
            c.Id != (SelectedCategory?.Id ?? Guid.Empty) &&
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = AppResources.ErrorCategoryDuplicate;
            return;
        }

        ErrorMessage = string.Empty;

        if (SelectedCategory is null)
        {
            await _categoryRepository.AddAsync(new Category { Id = Guid.NewGuid(), Name = name });
        }
        else
        {
            await _categoryRepository.UpdateAsync(new Category { Id = SelectedCategory.Id, Name = name });
        }

        await LoadAsync();
        NewCategoryName = string.Empty;
        SelectedCategory = null;
    }

    private Task EditAsync(CategoryWithCount? category)
    {
        if (category is not null)
        {
            SelectedCategory = category;
        }

        return Task.CompletedTask;
    }

    private async Task DeleteAsync(CategoryWithCount? category)
    {
        if (category is null)
        {
            return;
        }

        ErrorMessage = string.Empty;

        if (SelectedCategory?.Id == category.Id)
        {
            SelectedCategory = null;
            NewCategoryName = string.Empty;
        }

        await _categoryRepository.DeleteAsync(category.Id);
        await LoadAsync();
    }
}
