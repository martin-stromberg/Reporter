// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the feed detail page: shows the paged article list of a
/// single feed including its feed-level actions (refresh, rename, category,
/// edit, sync message, delete).
/// </summary>
public partial class FeedDetailViewModel : BaseViewModel
{
    /// <summary>
    /// The number of items loaded per page.
    /// </summary>
    public const int PageSize = 20;

    private static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly IItemRepository _itemRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly IFeedSyncService _feedSyncService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IKeywordRepository _keywordRepository;
    private readonly ILocalNotificationService? _localNotificationService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private Guid _feedId;
    private FeedListItem? _feed;
    private string _searchText = string.Empty;
    private string _errorMessage = string.Empty;
    private string _syncErrorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasMore = true;
    private int _currentPage;
    private bool _isSyncing;
    private bool _showEditForm;
    private CancellationTokenSource? _searchDebounceSource;
    private string _editUrl = string.Empty;
    private bool _editNotificationsEnabled = true;
    private string _newFeedKeywordText = string.Empty;
    private string _feedKeywordErrorMessage = string.Empty;
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedDetailViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="feedSyncService">The feed synchronization service.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    /// <param name="keywordRepository">The keyword repository used for the feed-scoped keyword list.</param>
    /// <param name="localNotificationService">The platform notification service, used to detect whether notifications are supported at all.</param>
    public FeedDetailViewModel(
        IItemRepository itemRepository,
        IFeedRepository feedRepository,
        IFeedSyncService feedSyncService,
        ICategoryRepository categoryRepository,
        INetworkStatusService networkStatusService,
        IKeywordRepository keywordRepository,
        ILocalNotificationService? localNotificationService = null)
    {
        _itemRepository = itemRepository;
        _feedRepository = feedRepository;
        _feedSyncService = feedSyncService;
        _categoryRepository = categoryRepository;
        _keywordRepository = keywordRepository;
        _localNotificationService = localNotificationService;
        InitConnectivity(networkStatusService);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => HasMore && !IsLoading);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsSyncing);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        ToggleSavedCommand = new AsyncRelayCommand<ItemListItem?>(ToggleSavedAsync);
        MarkReadCommand = new AsyncRelayCommand<ItemListItem?>(MarkReadAsync);
        EditCommand = new RelayCommand(Edit, () => HasFeed);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync);
        CloseEditFormCommand = new RelayCommand(ResetEditForm);
        AddFeedKeywordCommand = new AsyncRelayCommand(AddFeedKeywordAsync);
        RemoveFeedKeywordCommand = new AsyncRelayCommand<Keyword?>(RemoveFeedKeywordAsync);
    }

    /// <summary>
    /// Gets the command that loads the next page of articles.
    /// </summary>
    public AsyncRelayCommand LoadMoreCommand { get; }

    /// <summary>
    /// Gets the command that synchronizes this feed.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that navigates back to the feed list.
    /// </summary>
    public AsyncRelayCommand GoBackCommand { get; }

    /// <summary>
    /// Gets the command that toggles the saved-for-later state of an article.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> ToggleSavedCommand { get; }

    /// <summary>
    /// Gets the command that marks an article as read.
    /// </summary>
    public AsyncRelayCommand<ItemListItem?> MarkReadCommand { get; }

    /// <summary>
    /// Gets the command that opens the edit sheet for the current feed.
    /// </summary>
    public RelayCommand EditCommand { get; }

    /// <summary>
    /// Gets the command that persists the edit-sheet changes.
    /// </summary>
    public AsyncRelayCommand SaveEditCommand { get; }

    /// <summary>
    /// Gets the command that closes the edit sheet and resets its state.
    /// </summary>
    public RelayCommand CloseEditFormCommand { get; }

    /// <summary>
    /// Gets the command that adds a feed-scoped keyword from the edit-sheet input.
    /// </summary>
    public AsyncRelayCommand AddFeedKeywordCommand { get; }

    /// <summary>
    /// Gets the command that removes a feed-scoped keyword.
    /// </summary>
    public AsyncRelayCommand<Keyword?> RemoveFeedKeywordCommand { get; }

    /// <summary>
    /// Gets or sets the callback that performs the back navigation.
    /// Wired up by the page code-behind so the view model stays free of UI dependencies.
    /// </summary>
    public Func<Task>? NavigateBackAsync { get; set; }

    /// <summary>
    /// Gets the articles of the current feed.
    /// </summary>
    /// <returns>The observable collection of feed articles.</returns>
    public ObservableCollection<ItemListItem> Items { get; } = new();

    /// <summary>
    /// Gets the feed shown on this page, or <see langword="null"/> when the
    /// requested feed no longer exists.
    /// </summary>
    public FeedListItem? Feed
    {
        get => _feed;
        private set
        {
            if (SetProperty(ref _feed, value))
            {
                OnPropertyChanged(nameof(HasFeed));
                EditCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a feed is loaded.
    /// </summary>
    public bool HasFeed => _feed is not null;

    /// <summary>
    /// Gets or sets the list of available categories, including the
    /// <see cref="Guid.Empty"/> pseudo-entry for "no category".
    /// </summary>
    public ObservableCollection<Category> Categories
    {
        get => _categories;
        private set => SetProperty(ref _categories, value);
    }

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
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether more articles can be loaded.
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
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an error message is present.
    /// </summary>
    public bool HasError => _errorMessage.Length > 0;

    /// <summary>
    /// Gets or sets the current synchronization error message, shown above the article list.
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
    /// Gets or sets the title search text; every change restarts the paged list
    /// with the new filter after a short debounce delay.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                DebounceRestartList();
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
    /// Gets or sets a value indicating whether the edit sheet is visible.
    /// </summary>
    public bool ShowEditForm
    {
        get => _showEditForm;
        set => SetProperty(ref _showEditForm, value);
    }

    /// <summary>
    /// Gets or sets the URL edited in the edit sheet.
    /// </summary>
    public string EditUrl
    {
        get => _editUrl;
        set => SetProperty(ref _editUrl, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled for the edited feed.
    /// </summary>
    public bool EditNotificationsEnabled
    {
        get => _editNotificationsEnabled;
        set => SetProperty(ref _editNotificationsEnabled, value);
    }

    /// <summary>
    /// Gets a value indicating whether the current platform supports local notifications.
    /// When <c>false</c>, the per-feed notification switch is disabled with a platform hint.
    /// </summary>
    public bool NotificationsSupported => _localNotificationService?.IsSupported == true;

    /// <summary>
    /// Gets the feed-scoped keywords of the current feed.
    /// </summary>
    /// <returns>The observable collection of feed keywords.</returns>
    public ObservableCollection<Keyword> FeedKeywords { get; } = new();

    /// <summary>
    /// Gets or sets the text of the keyword input in the edit sheet; changing it clears the keyword error.
    /// </summary>
    public string NewFeedKeywordText
    {
        get => _newFeedKeywordText;
        set
        {
            if (SetProperty(ref _newFeedKeywordText, value))
            {
                FeedKeywordErrorMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets the validation error of the keyword input in the edit sheet.
    /// </summary>
    public string FeedKeywordErrorMessage
    {
        get => _feedKeywordErrorMessage;
        set
        {
            if (SetProperty(ref _feedKeywordErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasFeedKeywordError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a keyword error message is present.
    /// </summary>
    public bool HasFeedKeywordError => _feedKeywordErrorMessage.Length > 0;

    /// <summary>
    /// Loads the feed header, the categories and the first page of articles.
    /// An unknown feed id leaves <see cref="Feed"/> unset and reports a load error.
    /// </summary>
    /// <param name="feedId">The identifier of the feed to show.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadAsync(Guid feedId)
    {
        _feedId = feedId;

        try
        {
            Categories = await LoadCategoriesWithNoneAsync(_categoryRepository);
            Feed = (await _feedRepository.GetAllWithDetailsAsync()).FirstOrDefault(f => f.Id == feedId);

            FeedKeywords.Clear();
            var feedKeywords = await _keywordRepository.GetByFeedAsync(feedId);
            foreach (var keyword in feedKeywords)
            {
                FeedKeywords.Add(keyword);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorLoadFailed;
            return;
        }

        if (Feed is null)
        {
            ErrorMessage = AppResources.ErrorLoadFailed;
            return;
        }

        await ResetAndLoadFirstPageAsync();
    }

    /// <summary>
    /// Subscribes to connectivity change events and refreshes the current state.
    /// Called by the page while it is visible.
    /// </summary>
    public void AttachConnectivity()
    {
        TrackConnectivity();
        RefreshConnectivityStatus();
    }

    /// <summary>
    /// Unsubscribes from connectivity change events.
    /// Called by the page when it disappears.
    /// </summary>
    public void DetachConnectivity()
    {
        UntrackConnectivity();
    }

    /// <summary>
    /// Renames the feed after confirmation through the rename prompt.
    /// </summary>
    /// <param name="feed">The feed to rename.</param>
    /// <param name="newTitle">The new display title.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RenameFeedAsync(FeedListItem? feed, string? newTitle)
    {
        if (feed is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            ErrorMessage = AppResources.ErrorFeedTitleEmpty;
            return;
        }

        await _feedRepository.UpdateAsync(ToFeed(
            feed,
            url: feed.Url,
            title: newTitle.Trim(),
            categoryId: feed.CategoryId,
            notificationsEnabled: feed.NotificationsEnabled));
        ErrorMessage = string.Empty;
        await ReloadFeedAsync();
    }

    /// <summary>
    /// Assigns the feed to another category; the pseudo-category entry clears the assignment.
    /// </summary>
    /// <param name="feed">The feed to update.</param>
    /// <param name="category">The selected category, or the <see cref="Guid.Empty"/> pseudo-entry for none.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ChangeFeedCategoryAsync(FeedListItem? feed, Category? category)
    {
        if (feed is null || category is null)
        {
            return;
        }

        await _feedRepository.UpdateAsync(ToFeed(
            feed,
            url: feed.Url,
            title: feed.Title,
            categoryId: category.Id == Guid.Empty ? null : category.Id,
            notificationsEnabled: feed.NotificationsEnabled));
        await ReloadFeedAsync();
    }

    /// <summary>
    /// Deletes the current feed after confirmation and navigates back to the feed list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteFeedAsync()
    {
        if (Feed is null)
        {
            return;
        }

        await _feedRepository.DeleteAsync(Feed.Id);
        await GoBackAsync();
    }

    /// <summary>
    /// Builds the localized message text for the feed's last sync error or
    /// warning: the stored <see cref="FeedListItem.LastMessageKind"/> maps to a
    /// <c>FeedErrorKind*</c> or <c>FeedWarningKind*</c> resource and the raw
    /// technical message is appended as a second paragraph when present.
    /// </summary>
    /// <param name="feed">The feed whose message should be described.</param>
    /// <returns>The localized message text, optionally followed by the raw message.</returns>
    public string GetFeedMessage(FeedListItem feed)
    {
        var kindText = feed.LastMessageKind switch
        {
            FeedSyncErrorKind.InsecureHttpBlocked => AppResources.FeedErrorKindInsecureHttpBlocked,
            FeedSyncErrorKind.HttpStatus => AppResources.FeedErrorKindHttpStatus,
            FeedSyncErrorKind.Network => AppResources.FeedErrorKindNetwork,
            FeedSyncErrorKind.Parse => AppResources.FeedErrorKindParse,
            FeedSyncWarningKind.FewerItems => AppResources.FeedWarningKindFewerItems,
            FeedSyncWarningKind.NoRecentItems => AppResources.FeedWarningKindNoRecentItems,
            _ => feed.HealthStatus == FeedHealth.Warning
                ? AppResources.FeedWarningKindUnknown
                : AppResources.FeedErrorKindUnknown,
        };

        return string.IsNullOrWhiteSpace(feed.LastMessage)
            ? kindText
            : $"{kindText}\n\n{feed.LastMessage}";
    }

    /// <summary>
    /// Builds the action-sheet option labels for the given names, suffixing
    /// repeated names so every returned label is unique and maps back to
    /// exactly one entry. Candidates are checked against all already assigned
    /// labels — a literal name like "News (2)" can collide with a generated
    /// suffix, so the counter is raised until the label is unused.
    /// </summary>
    /// <param name="names">The option names in display order.</param>
    /// <returns>The labels, aligned by index with <paramref name="names"/>.</returns>
    public static List<string> MakeUniqueOptionLabels(IReadOnlyList<string> names)
    {
        var usedLabels = new HashSet<string>(StringComparer.Ordinal);
        var labels = new List<string>(names.Count);
        foreach (var name in names)
        {
            var label = name;
            for (var suffix = 2; !usedLabels.Add(label); suffix++)
            {
                label = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, suffix);
            }

            labels.Add(label);
        }

        return labels;
    }

    /// <inheritdoc />
    protected override void OnConnectivityChanged(bool isOnline)
    {
        SyncErrorMessage = string.Empty;
    }

    /// <inheritdoc />
    protected override void SetIsSyncing(bool isSyncing)
    {
        IsSyncing = isSyncing;
    }

    /// <inheritdoc />
    protected override void ReportSyncError(string message)
    {
        SyncErrorMessage = message;
    }

    private string? SearchTermOrNull =>
        string.IsNullOrWhiteSpace(_searchText) ? null : _searchText.Trim();

    // Debounces the list restart per keystroke: every change cancels the pending
    // restart so rapid input triggers a single reload instead of one database
    // query per character.
    private void DebounceRestartList()
    {
        var previous = _searchDebounceSource;
        _searchDebounceSource = new CancellationTokenSource();
        // Cancel resolves the pending Task.Delay before the superseded source
        // is disposed, so the disposal cannot fault the abandoned wait.
        previous?.Cancel();
        previous?.Dispose();
        _ = RestartListDebouncedAsync(_searchDebounceSource);
    }

    private async Task RestartListDebouncedAsync(CancellationTokenSource source)
    {
        try
        {
            await Task.Delay(SearchDebounceDelay, source.Token);
            await RestartListAsync();
        }
        catch (OperationCanceledException)
        {
            // A newer change superseded this restart.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RestartListDebouncedAsync failed: {ex}");
        }
    }

    private async Task RestartListAsync()
    {
        if (Feed is null)
        {
            return;
        }

        await ResetAndLoadFirstPageAsync();
    }

    private async Task ResetAndLoadFirstPageAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            ErrorMessage = string.Empty;
            _currentPage = 0;
            HasMore = true;
            Items.Clear();
            await LoadPageCoreAsync();
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

            await LoadPageCoreAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadPageCoreAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _itemRepository.GetByFeedAsync(_feedId, _currentPage, PageSize, SearchTermOrNull);
            foreach (var item in items)
            {
                Items.Add(item);
            }

            _currentPage++;
            HasMore = items.Count == PageSize;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadPageCoreAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorLoadFailed;
            HasMore = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<bool> ReloadFeedAsync()
    {
        try
        {
            Feed = (await _feedRepository.GetAllWithDetailsAsync()).FirstOrDefault(f => f.Id == _feedId);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ReloadFeedAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorLoadFailed;
            return false;
        }
    }

    private async Task RefreshAsync()
    {
        if (await RunFeedSyncAsync(() => _feedSyncService.SyncFeedAsync(_feedId)) &&
            await ReloadFeedAsync())
        {
            await RestartListAsync();
        }
    }

    private async Task GoBackAsync()
    {
        if (NavigateBackAsync is not null)
        {
            await NavigateBackAsync();
        }
    }

    private void Edit()
    {
        if (Feed is null)
        {
            return;
        }

        EditUrl = Feed.Url;
        EditNotificationsEnabled = Feed.NotificationsEnabled;
        NewFeedKeywordText = string.Empty;
        FeedKeywordErrorMessage = string.Empty;
        ErrorMessage = string.Empty;
        ShowEditForm = true;
    }

    private async Task SaveEditAsync()
    {
        var feed = Feed;
        if (feed is null)
        {
            return;
        }

        var url = EditUrl.Trim();
        if (!FeedUrlValidator.IsValidFeedUrl(url))
        {
            ErrorMessage = AppResources.ErrorFeedUrlInvalid;
            return;
        }

        try
        {
            var existing = await _feedRepository.GetByUrlAsync(url);
            if (existing is not null && existing.Id != feed.Id)
            {
                ErrorMessage = AppResources.ErrorFeedDuplicate;
                return;
            }

            await _feedRepository.UpdateAsync(ToFeed(
                feed,
                url: url,
                title: feed.Title,
                categoryId: feed.CategoryId,
                notificationsEnabled: EditNotificationsEnabled));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SaveEditAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorActionFailed;
            return;
        }

        ResetEditForm();
        await ReloadFeedAsync();
    }

    private void ResetEditForm()
    {
        ShowEditForm = false;
        EditUrl = string.Empty;
        EditNotificationsEnabled = true;
        NewFeedKeywordText = string.Empty;
        FeedKeywordErrorMessage = string.Empty;
        ErrorMessage = string.Empty;
    }

    private async Task ToggleSavedAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.ToggleSavedForLaterAsync(item.Id);

        var index = Items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        Items[index] = item.CopyWith(isSavedForLater: !item.IsSavedForLater);
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);

        var index = Items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        Items[index] = item.CopyWith(isRead: true);
    }

    private async Task AddFeedKeywordAsync()
    {
        // Without a loaded feed there is no valid feed id — the command cannot
        // run from the UI in that state (EditCommand is gated on HasFeed), but
        // the guard keeps the method safe against programmatic calls.
        if (!HasFeed)
        {
            return;
        }

        var text = NewFeedKeywordText?.Trim() ?? string.Empty;
        if (!KeywordValidator.TryValidate(text, FeedKeywords, out var validationError))
        {
            FeedKeywordErrorMessage = validationError;
            return;
        }

        var keyword = new Keyword { Id = Guid.NewGuid(), KeywordText = text, FeedId = _feedId };
        try
        {
            await _keywordRepository.AddAsync(keyword);
            FeedKeywords.Add(keyword);
            NewFeedKeywordText = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add feed keyword: {ex}");
            FeedKeywordErrorMessage = AppResources.ErrorActionFailed;
        }
    }

    private async Task RemoveFeedKeywordAsync(Keyword? keyword)
    {
        if (keyword is null)
        {
            return;
        }

        try
        {
            await _keywordRepository.DeleteAsync(keyword.Id);
            FeedKeywords.Remove(keyword);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove feed keyword: {ex}");
            FeedKeywordErrorMessage = AppResources.ErrorActionFailed;
        }
    }

    // Rebuilds the stored feed from its list item for partial updates so the
    // untouched fields survive; url, title, category id and the notifications
    // flag are supplied by the caller.
    private static Feed ToFeed(FeedListItem feed, string url, string title, Guid? categoryId, bool notificationsEnabled)
    {
        return new Feed
        {
            Id = feed.Id,
            Url = url,
            Title = title,
            CategoryId = categoryId,
            LastCheckedAt = feed.LastCheckedAt,
            HealthStatus = feed.HealthStatus,
            HealthLastChange = feed.HealthLastChange,
            NotificationsEnabled = notificationsEnabled,
            FaviconUrl = feed.FaviconUrl,
            LastMessageKind = feed.LastMessageKind,
            LastMessage = feed.LastMessage,
        };
    }
}
