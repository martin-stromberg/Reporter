// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the later articles page.
/// </summary>
public partial class LaterViewModel : BaseViewModel
{
    /// <summary>
    /// The number of saved items loaded per page.
    /// </summary>
    public const int PageSize = 20;

    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private string _title = AppResources.PageTitleLater;
    private bool _isLoading;
    private bool _hasMore = true;
    private int _currentPage;
    private string _errorMessage = string.Empty;
    private ObservableCollection<CategoryFilterItem> _categories = [];
    private CategoryFilterItem? _selectedCategory;
    private int _savedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="categoryRepository">The category repository used for the filter tabs.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    public LaterViewModel(IItemRepository itemRepository, ICategoryRepository categoryRepository, INetworkStatusService networkStatusService)
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        TrackConnectivity(networkStatusService);
        SavedItems.CollectionChanged += OnSavedItemsChanged;
        SelectCategoryCommand = new AsyncRelayCommand<CategoryFilterItem?>(SelectCategoryAsync);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => HasMore && !IsLoading);
        ToggleSavedCommand = new AsyncRelayCommand<ItemListItem?>(ToggleSavedAsync);
        MarkReadCommand = new AsyncRelayCommand<ItemListItem?>(MarkReadAsync);
    }

    /// <summary>
    /// Gets the command that loads the first page of saved-for-later articles.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that loads the next page of saved-for-later articles.
    /// </summary>
    public AsyncRelayCommand LoadMoreCommand { get; }

    /// <summary>
    /// Gets the command that toggles the saved-for-later state of an article.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> ToggleSavedCommand { get; }

    /// <summary>
    /// Gets the command that marks an article as read.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> MarkReadCommand { get; }

    /// <summary>
    /// Gets the command that selects a category filter tab.
    /// </summary>
    public AsyncRelayCommand<CategoryFilterItem?> SelectCategoryCommand { get; }

    /// <summary>
    /// Gets the category filter tabs ("All" first, then every category with saved articles).
    /// </summary>
    public ObservableCollection<CategoryFilterItem> Categories
    {
        get => _categories;
        private set
        {
            if (SetProperty(ref _categories, value))
            {
                OnPropertyChanged(nameof(ShowCategoryTabs));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the filter tabs are shown (only when at least one category has saved articles).
    /// </summary>
    public bool ShowCategoryTabs => _categories.Count > 1;

    /// <summary>
    /// Gets the selected category filter tab.
    /// </summary>
    public CategoryFilterItem? SelectedCategory
    {
        get => _selectedCategory;
        private set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                foreach (var category in _categories)
                {
                    category.IsSelected = category.CategoryId == value?.CategoryId;
                }
            }
        }
    }

    /// <summary>
    /// Gets the total number of saved articles over all categories.
    /// </summary>
    public int SavedCount
    {
        get => _savedCount;
        private set
        {
            if (SetProperty(ref _savedCount, value))
            {
                OnPropertyChanged(nameof(SavedCountText));
            }
        }
    }

    /// <summary>
    /// Gets the localized saved counter (e.g. "7 saved").
    /// </summary>
    public string SavedCountText
    {
        get => string.Format(CultureInfo.CurrentCulture, AppResources.LaterSavedCountFormat, _savedCount);
    }

    /// <summary>
    /// Gets a value indicating whether at least one saved article is listed.
    /// </summary>
    public bool HasItems => SavedItems.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the loading skeleton is shown (loading without content).
    /// </summary>
    public bool ShowSkeleton => IsLoading && !HasItems;

    /// <summary>
    /// Gets a value indicating whether the empty state is shown.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasItems && !HasError;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets the items saved for later.
    /// </summary>
    /// <returns>The observable collection of saved items.</returns>
    public ObservableCollection<ItemListItem> SavedItems { get; } = new();

    /// <summary>
    /// Gets a value indicating whether a page is currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadMoreCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ShowSkeleton));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether more saved items can be loaded.
    /// </summary>
    public bool HasMore
    {
        get => _hasMore;
        private set
        {
            if (SetProperty(ref _hasMore, value))
            {
                LoadMoreCommand.NotifyCanExecuteChanged();
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
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an error message is present.
    /// </summary>
    public bool HasError => _errorMessage.Length > 0;

    private async Task LoadAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            ErrorMessage = string.Empty;
            _currentPage = 0;
            HasMore = true;
            await LoadCategoriesAsync();

            // Erst nach dem Laden ersetzen, damit die Liste beim Wiederbetreten nicht leer aufblitzt.
            await LoadPageCoreAsync(replace: true);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadMoreAsync()
    {
        if (IsLoading || !HasMore)
        {
            return;
        }

        await _loadLock.WaitAsync();
        try
        {
            if (IsLoading || !HasMore)
            {
                return;
            }

            await LoadPageCoreAsync(replace: false);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadPageCoreAsync(bool replace)
    {
        IsLoading = true;
        try
        {
            var items = await _itemRepository.GetSavedForLaterAsync(_currentPage, PageSize, SelectedCategory?.CategoryId);
            if (replace)
            {
                SavedItems.Clear();
            }

            foreach (var item in items)
            {
                SavedItems.Add(item);
            }

            _currentPage++;
            HasMore = items.Count == PageSize;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadPageCoreAsync failed: {ex}");
            if (replace)
            {
                SavedItems.Clear();
            }

            ErrorMessage = AppResources.ErrorLoadFailed;
            HasMore = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleSavedAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.ToggleSavedForLaterAsync(item.Id);
        var listed = SavedItems.FirstOrDefault(i => i.Id == item.Id);
        if (listed is null)
        {
            return;
        }

        SavedItems.Remove(listed);
        SavedCount = Math.Max(0, SavedCount - 1);
        foreach (var chip in _categories.Where(c => c.CategoryId is null || c.CategoryId == listed.CategoryId))
        {
            chip.Count = Math.Max(0, chip.Count - 1);
        }
    }

    private async Task SelectCategoryAsync(CategoryFilterItem? category)
    {
        if (category is null)
        {
            return;
        }

        SelectedCategory = category;
        await _loadLock.WaitAsync();
        try
        {
            ErrorMessage = string.Empty;
            _currentPage = 0;
            HasMore = true;
            await LoadPageCoreAsync(replace: true);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var all = new CategoryFilterItem { CategoryId = null, Name = AppResources.FilterAll };
            var filters = new List<CategoryFilterItem> { all };
            foreach (var category in await _categoryRepository.GetAllAsync())
            {
                var count = await _itemRepository.GetSavedCountAsync(category.Id);
                if (count > 0 || category.Id == SelectedCategory?.CategoryId)
                {
                    filters.Add(new CategoryFilterItem { CategoryId = category.Id, Name = category.Name, Count = count });
                }
            }

            all.Count = await _itemRepository.GetSavedCountAsync(null);
            SavedCount = all.Count;
            Categories = new ObservableCollection<CategoryFilterItem>(filters);
            var selected = filters.FirstOrDefault(f => f.CategoryId == SelectedCategory?.CategoryId) ?? all;
            _selectedCategory = null;
            SelectedCategory = selected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadCategoriesAsync failed: {ex}");
        }
    }

    private void OnSavedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ShowSkeleton));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);

        var index = SavedItems.IndexOf(SavedItems.FirstOrDefault(i => i.Id == item.Id) ?? item);
        if (index < 0)
        {
            return;
        }

        SavedItems[index] = item.CopyWith(isRead: true);
    }
}
