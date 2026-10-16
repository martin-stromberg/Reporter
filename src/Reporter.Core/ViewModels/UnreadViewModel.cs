// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
    private readonly ISettingsRepository _settingsRepository;

    private string _title = AppResources.PageTitleUnread;
    private ObservableCollection<ItemListItem> _articles = [];
    private ObservableCollection<CategoryFilterItem> _categories = [];
    private CategoryFilterItem? _selectedCategory;
    private bool _isSyncing;
    private bool _isLoading;
    private bool _hasMore;
    private string _errorMessage = string.Empty;
    private string _lastSyncText = string.Empty;
    private string _unreadCountText = string.Empty;
    private int _currentPage;
    private string _syncErrorMessage = string.Empty;
    private string _listLayout = SettingsValues.UnreadLayoutList;
    private DateTime? _lastSyncAtUtc;
    private int _cardPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="feedSyncService">The feed synchronization service.</param>
    /// <param name="settingsRepository">The settings repository used for the unread sort order.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    public UnreadViewModel(IItemRepository itemRepository, ICategoryRepository categoryRepository, IFeedSyncService feedSyncService, ISettingsRepository settingsRepository, INetworkStatusService networkStatusService)
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _feedSyncService = feedSyncService;
        _settingsRepository = settingsRepository;
        TrackConnectivity(networkStatusService);

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => !IsLoading && HasMore);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        MarkAllReadCommand = new AsyncRelayCommand(MarkAllReadAsync);
        SelectCategoryCommand = new AsyncRelayCommand<CategoryFilterItem?>(SelectCategoryAsync);
        ToggleSavedCommand = new AsyncRelayCommand<ItemListItem?>(ToggleSavedAsync);
        MarkReadCommand = new AsyncRelayCommand<ItemListItem?>(MarkReadAsync);
        ToggleLayoutCommand = new AsyncRelayCommand(ToggleLayoutAsync);
        NextCardCommand = new RelayCommand(GoToNextCard, () => CanGoToNextCard);
        PreviousCardCommand = new RelayCommand(GoToPreviousCard, () => CanGoToPreviousCard);
        _articles.CollectionChanged += OnArticlesCollectionChanged;
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
    /// Gets the command that switches between the list and the card layout and persists the choice.
    /// </summary>
    public AsyncRelayCommand ToggleLayoutCommand { get; }

    /// <summary>
    /// Gets the command that advances the card stack to the next article.
    /// </summary>
    public RelayCommand NextCardCommand { get; }

    /// <summary>
    /// Gets the command that moves the card stack back to the previous article.
    /// </summary>
    public RelayCommand PreviousCardCommand { get; }

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
        set
        {
            var previous = _articles;
            var currentId = CurrentCard?.Id;
            if (SetProperty(ref _articles, value))
            {
                previous.CollectionChanged -= OnArticlesCollectionChanged;
                value.CollectionChanged += OnArticlesCollectionChanged;

                // Beim Neuladen bleibt der Kartenstapel auf demselben Artikel, sofern er noch existiert.
                var index = currentId is null ? -1 : IndexOfArticle(currentId.Value);
                _cardPosition = index >= 0 ? index : 0;
                NotifyListStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the article list contains at least one article.
    /// </summary>
    public bool HasArticles => _articles.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the loading skeleton is shown (first load without content).
    /// </summary>
    public bool ShowSkeleton => IsLoading && !HasArticles;

    /// <summary>
    /// Gets a value indicating whether the "all caught up" empty state is shown.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasArticles && !HasError;

    /// <summary>
    /// Gets the persisted layout of the start page ("list" or "cards").
    /// </summary>
    public string ListLayout
    {
        get => _listLayout;
        private set
        {
            if (SetProperty(ref _listLayout, SettingsValues.NormalizeUnreadLayout(value)))
            {
                OnPropertyChanged(nameof(IsCardLayout));
                OnPropertyChanged(nameof(IsListLayout));
                OnPropertyChanged(nameof(LayoutToggleDescription));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether articles are shown as swipeable cards.
    /// </summary>
    public bool IsCardLayout => _listLayout == SettingsValues.UnreadLayoutCards;

    /// <summary>
    /// Gets a value indicating whether articles are shown as a scrolling list.
    /// </summary>
    public bool IsListLayout => !IsCardLayout;

    /// <summary>
    /// Gets the localized screen-reader description of the layout toggle.
    /// </summary>
    public string LayoutToggleDescription => IsCardLayout
        ? AppResources.UnreadLayoutSwitchToList
        : AppResources.UnreadLayoutSwitchToCards;

    /// <summary>
    /// Gets the localized weekday and date shown as eyebrow above the page title.
    /// </summary>
    public string HeaderDateText
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            return DateTime.Now.ToString("dddd, " + culture.DateTimeFormat.MonthDayPattern, culture);
        }
    }

    /// <summary>
    /// Gets the compact unread counter for the selected filter (e.g. "42 unread").
    /// </summary>
    public string UnreadBadgeText
    {
        get => string.Format(CultureInfo.CurrentCulture, AppResources.UnreadCountFormat, UnreadCount);
    }

    /// <summary>
    /// Gets the synchronization status line (syncing, offline or the relative time of the last sync).
    /// </summary>
    public string SyncStatusText
    {
        get
        {
            if (IsSyncing)
            {
                return AppResources.UnreadSyncing;
            }

            if (!IsOnline)
            {
                return AppResources.UnreadOffline;
            }

            return _lastSyncAtUtc is null
                ? string.Empty
                : string.Format(CultureInfo.CurrentCulture, AppResources.UnreadSyncedFormat, RelativeTimeFormatter.Format(_lastSyncAtUtc, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Gets the article on top of the card stack, or <c>null</c> when the list is empty.
    /// </summary>
    public ItemListItem? CurrentCard => _cardPosition < _articles.Count ? _articles[_cardPosition] : null;

    /// <summary>
    /// Gets the article waiting underneath the top card, or <c>null</c> at the end of the stack.
    /// </summary>
    public ItemListItem? NextCard => _cardPosition + 1 < _articles.Count ? _articles[_cardPosition + 1] : null;

    /// <summary>
    /// Gets a value indicating whether the card stack can advance.
    /// </summary>
    public bool CanGoToNextCard => _cardPosition + 1 < _articles.Count;

    /// <summary>
    /// Gets a value indicating whether the card stack can go back.
    /// </summary>
    public bool CanGoToPreviousCard => _cardPosition > 0 && _articles.Count > 0;

    /// <summary>
    /// Gets or sets the zero-based position of the top card; values are clamped to the list.
    /// </summary>
    public int CardPosition
    {
        get => _cardPosition;
        set
        {
            var clamped = Math.Clamp(value, 0, Math.Max(0, _articles.Count - 1));
            if (SetProperty(ref _cardPosition, clamped))
            {
                NotifyCardStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets the localized card position indicator (e.g. "3 of 42").
    /// </summary>
    public string CardPositionText => HasArticles
        ? string.Format(CultureInfo.CurrentCulture, AppResources.UnreadCardPositionFormat, Math.Min(_cardPosition + 1, _articles.Count), Math.Max(UnreadCount, _articles.Count))
        : string.Empty;

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
                OnPropertyChanged(nameof(SyncStatusText));
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
                OnPropertyChanged(nameof(ShowSkeleton));
                OnPropertyChanged(nameof(ShowEmptyState));
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
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an error message is present.
    /// </summary>
    public bool HasError => _errorMessage.Length > 0;

    /// <summary>
    /// Gets or sets the current synchronization error message, shown above the article list.
    /// Kept separate from <see cref="ErrorMessage"/> so connectivity changes only clear
    /// sync feedback and do not remove load errors.
    /// </summary>
    public string SyncErrorMessage
    {
        get => _syncErrorMessage;
        set
        {
            if (SetProperty(ref _syncErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasSyncError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a synchronization error message is present.
    /// </summary>
    public bool HasSyncError => _syncErrorMessage.Length > 0;

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

    /// <summary>
    /// Gets or sets the formatted unread count text.
    /// </summary>
    public string UnreadCountText
    {
        get => _unreadCountText;
        set => SetProperty(ref _unreadCountText, value);
    }

    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        SyncErrorMessage = string.Empty;
        _currentPage = 0;
        HasMore = false;
        OnPropertyChanged(nameof(HeaderDateText));
        OnPropertyChanged(nameof(SyncStatusText));

        var settings = await _settingsRepository.GetAsync();
        ListLayout = settings.UnreadListLayout ?? SettingsValues.UnreadLayoutList;

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
            var settings = await _settingsRepository.GetAsync();
            var ascending = settings.UnreadSortOrder == SettingsValues.SortOrderAscending;
            var items = await _itemRepository.GetUnreadByDateAsync(page, PageSize, categoryId, ascending);

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
                UpdateUnreadCountText();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadPageAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorLoadFailed;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (!IsOnline)
        {
            // The RefreshView.IsRefreshing TwoWay binding may already have pushed
            // IsSyncing to true before the command ran — reset it so the refresh
            // indicator does not hang. No error is raised; the persistent offline
            // hint already communicates the state.
            IsSyncing = false;
            return;
        }

        IsSyncing = true;
        SyncErrorMessage = string.Empty;

        var syncError = string.Empty;
        try
        {
            var result = await _feedSyncService.SyncAllAsync();
            if (result.Status == FeedHealth.Error)
            {
                // The technical detail stays in the SyncLog (persisted via
                // FeedSyncService.UpdateLogAsync); the UI shows the localized
                // generic message instead of raw English/exception text.
                syncError = AppResources.SyncStatusError;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RefreshAsync failed: {ex}");
            syncError = AppResources.SyncStatusError;
        }
        finally
        {
            IsSyncing = false;
        }

        _lastSyncAtUtc = DateTime.UtcNow;
        LastSyncText = string.Format(CultureInfo.CurrentCulture, "{0:g}", DateTime.Now);
        OnPropertyChanged(nameof(SyncStatusText));
        await LoadAsync();

        if (syncError.Length > 0)
        {
            SyncErrorMessage = syncError;
        }
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
            Articles[index] = updated.CopyWith(isSavedForLater: !updated.IsSavedForLater);
        }
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);
        var listed = Articles.FirstOrDefault(a => a.Id == item.Id);
        if (listed is not null)
        {
            Articles.Remove(listed);
        }

        if (SelectedCategory is not null)
        {
            SelectedCategory.Count--;
        }

        // Die übrigen betroffenen Chips („Alle" bzw. die Kategorie des Artikels) konsistent mitzählen.
        foreach (var chip in Categories.Where(c => c != SelectedCategory && (c.CategoryId is null || c.CategoryId == item.CategoryId)))
        {
            chip.Count = Math.Max(0, chip.Count - 1);
        }

        UnreadCount = Math.Max(0, UnreadCount - 1);
        UpdateUnreadCountText();

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

    private async Task ToggleLayoutAsync()
    {
        ListLayout = IsCardLayout ? SettingsValues.UnreadLayoutList : SettingsValues.UnreadLayoutCards;

        try
        {
            var settings = await _settingsRepository.GetAsync();
            await _settingsRepository.SaveAsync(settings.WithUnreadListLayout(ListLayout));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ToggleLayoutAsync failed: {ex}");
        }
    }

    private void GoToNextCard()
    {
        if (!CanGoToNextCard)
        {
            return;
        }

        CardPosition++;
        if (_cardPosition >= _articles.Count - 3 && LoadMoreCommand.CanExecute(null))
        {
            LoadMoreCommand.Execute(null);
        }
    }

    private void GoToPreviousCard()
    {
        if (CanGoToPreviousCard)
        {
            CardPosition--;
        }
    }

    private int IndexOfArticle(Guid id)
    {
        for (var i = 0; i < _articles.Count; i++)
        {
            if (_articles[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnArticlesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _cardPosition = Math.Clamp(_cardPosition, 0, Math.Max(0, _articles.Count - 1));
        NotifyListStateChanged();
    }

    private void NotifyListStateChanged()
    {
        OnPropertyChanged(nameof(HasArticles));
        OnPropertyChanged(nameof(ShowSkeleton));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(CardPosition));
        NotifyCardStateChanged();
    }

    private void NotifyCardStateChanged()
    {
        OnPropertyChanged(nameof(CurrentCard));
        OnPropertyChanged(nameof(NextCard));
        OnPropertyChanged(nameof(CanGoToNextCard));
        OnPropertyChanged(nameof(CanGoToPreviousCard));
        OnPropertyChanged(nameof(CardPositionText));
        NextCardCommand.NotifyCanExecuteChanged();
        PreviousCardCommand.NotifyCanExecuteChanged();
    }

    private void UpdateUnreadCountText()
    {
        UnreadCountText = string.IsNullOrEmpty(LastSyncText)
            ? $"{UnreadCount} {AppResources.LabelUnreadArticles}"
            : $"{UnreadCount} {AppResources.LabelUnreadArticles} • {LastSyncText}";
        OnPropertyChanged(nameof(UnreadBadgeText));
        OnPropertyChanged(nameof(CardPositionText));
    }

    /// <inheritdoc />
    protected override void OnConnectivityChanged(bool isOnline)
    {
        SyncErrorMessage = string.Empty;
        OnPropertyChanged(nameof(SyncStatusText));
    }
}
