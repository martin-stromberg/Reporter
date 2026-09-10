using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the unread articles dashboard.
/// </summary>
public partial class UnreadViewModel : BaseViewModel
{
    private const int PageSize = 20;

    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFeedSyncService _feedSyncService;

    private string _title = AppResources.PageTitleUnread;
    private ObservableCollection<ItemListItem> _articles = [];
    private ObservableCollection<CategoryFilterItem> _categories = [];
    private CategoryFilterItem? _selectedCategory;
    private bool _isSyncing;
    private bool _isLoading;
    private bool _hasMore;
    private string _errorMessage = string.Empty;
    private string _lastSyncText = string.Empty;
    private int _currentPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="feedSyncService">The feed synchronization service.</param>
    public UnreadViewModel(IItemRepository itemRepository, ICategoryRepository categoryRepository, IFeedSyncService feedSyncService)
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _feedSyncService = feedSyncService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => !IsLoading && HasMore);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        MarkAllReadCommand = new AsyncRelayCommand(MarkAllReadAsync);
        SelectCategoryCommand = new AsyncRelayCommand<CategoryFilterItem?>(SelectCategoryAsync);
        ToggleSavedCommand = new AsyncRelayCommand<ItemListItem?>(ToggleSavedAsync);
        MarkReadCommand = new AsyncRelayCommand<ItemListItem?>(MarkReadAsync);
    }

    /// <summary>
    /// Gets the command that loads the first page of unread articles.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that loads the next page of unread articles.
    /// </summary>
    public AsyncRelayCommand LoadMoreCommand { get; }

    /// <summary>
    /// Gets the command that synchronizes all feeds and reloads the list.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that marks all unread articles as read.
    /// </summary>
    public AsyncRelayCommand MarkAllReadCommand { get; }

    /// <summary>
    /// Gets the command that selects a category filter.
    /// </summary>
    public AsyncRelayCommand<CategoryFilterItem?> SelectCategoryCommand { get; }

    /// <summary>
    /// Gets the command that toggles the saved-for-later state of an article.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> ToggleSavedCommand { get; }

    /// <summary>
    /// Gets the command that marks an article as read.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> MarkReadCommand { get; }

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the list of unread articles.
    /// </summary>
    public ObservableCollection<ItemListItem> Articles
    {
        get => _articles;
        set => SetProperty(ref _articles, value);
    }

    /// <summary>
    /// Gets or sets the list of category filter chips.
    /// </summary>
    public ObservableCollection<CategoryFilterItem> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    public CategoryFilterItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                UpdateCategorySelection();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a synchronization is in progress.
    /// </summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        set
        {
            if (SetProperty(ref _isSyncing, value))
            {
                RefreshCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether more articles are being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadMoreCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether more pages are available.
    /// </summary>
    public bool HasMore
    {
        get => _hasMore;
        set => SetProperty(ref _hasMore, value);
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
    /// Gets or sets the human-readable last sync time.
    /// </summary>
    public string LastSyncText
    {
        get => _lastSyncText;
        set => SetProperty(ref _lastSyncText, value);
    }

    /// <summary>
    /// Gets or sets the total number of unread articles for the selected filter.
    /// </summary>
    public int UnreadCount { get; private set; }

    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        _currentPage = 0;
        Articles = [];
        HasMore = false;

        var categories = await _categoryRepository.GetAllAsync();
        var filterCategories = new List<CategoryFilterItem>
        {
            new CategoryFilterItem { CategoryId = null, Name = AppResources.FilterAll, Count = 0 },
        };

        foreach (var category in categories)
        {
            var count = await _itemRepository.GetUnreadCountAsync(category.Id);
            filterCategories.Add(new CategoryFilterItem
            {
                CategoryId = category.Id,
                Name = category.Name,
                Count = count,
            });
        }

        var allCount = await _itemRepository.GetUnreadCountAsync(null);
        filterCategories[0].Count = allCount;
        UnreadCount = allCount;

        Categories = new ObservableCollection<CategoryFilterItem>(filterCategories);

        if (SelectedCategory is null || Categories.All(c => c.CategoryId != SelectedCategory.CategoryId))
        {
            SelectedCategory = Categories.First();
        }
        else
        {
            UpdateCategorySelection();
        }

        await LoadPageAsync(_currentPage);
    }

    private async Task LoadMoreAsync()
    {
        if (IsLoading || !HasMore)
        {
            return;
        }

        _currentPage++;
        await LoadPageAsync(_currentPage, append: true);
    }

    private async Task LoadPageAsync(int page, bool append = false)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var categoryId = SelectedCategory?.CategoryId;
            var items = await _itemRepository.GetUnreadByDateAsync(page, PageSize, categoryId);

            if (append)
            {
                foreach (var item in items)
                {
                    Articles.Add(item);
                }
            }
            else
            {
                Articles = new ObservableCollection<ItemListItem>(items);
            }

            HasMore = items.Count == PageSize;

            if (SelectedCategory is not null)
            {
                var updatedCount = await _itemRepository.GetUnreadCountAsync(SelectedCategory.CategoryId);
                SelectedCategory.Count = updatedCount;
                UnreadCount = updatedCount;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsSyncing = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _feedSyncService.SyncAllAsync();
            if (result.Status == FeedHealth.Error)
            {
                ErrorMessage = result.Message ?? AppResources.SyncStatusError;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSyncing = false;
        }

        LastSyncText = string.Format(CultureInfo.CurrentCulture, "{0:g}", DateTime.Now);
        await LoadAsync();
    }

    private async Task MarkAllReadAsync()
    {
        ErrorMessage = string.Empty;
        await _itemRepository.MarkAllAsReadAsync(SelectedCategory?.CategoryId);
        await LoadAsync();
    }

    private async Task SelectCategoryAsync(CategoryFilterItem? category)
    {
        if (category is null)
        {
            return;
        }

        SelectedCategory = category;
        _currentPage = 0;
        Articles = [];
        HasMore = false;
        await LoadPageAsync(_currentPage);
    }

    private async Task ToggleSavedAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.ToggleSavedForLaterAsync(item.Id);
        var updated = Articles.FirstOrDefault(a => a.Id == item.Id);
        if (updated is not null)
        {
            var index = Articles.IndexOf(updated);
            Articles[index] = new ItemListItem
            {
                Id = updated.Id,
                FeedId = updated.FeedId,
                Title = updated.Title,
                Link = updated.Link,
                PublishedAt = updated.PublishedAt,
                IsRead = updated.IsRead,
                IsSavedForLater = !updated.IsSavedForLater,
                FeedTitle = updated.FeedTitle,
                CategoryId = updated.CategoryId,
                CategoryName = updated.CategoryName,
            };
        }
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);
        Articles.Remove(item);

        if (SelectedCategory is not null)
        {
            SelectedCategory.Count--;
        }

        if (Articles.Count == 0)
        {
            await LoadAsync();
        }
    }

    private void UpdateCategorySelection()
    {
        if (Categories is null || SelectedCategory is null)
        {
            return;
        }

        foreach (var category in Categories)
        {
            category.IsSelected = category.CategoryId == SelectedCategory.CategoryId;
        }
    }
}
