// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for managing feeds.
/// </summary>
public partial class FeedsViewModel : BaseViewModel
{
    private readonly IFeedRepository _feedRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFeedSyncService _feedSyncService;
    private readonly ILocalNotificationService? _localNotificationService;

    private string _newUrl = string.Empty;
    private string _newTitle = string.Empty;
    private bool _feedNotificationsEnabled = true;
    private string _errorMessage = string.Empty;
    private string _syncErrorMessage = string.Empty;
    private bool _isSyncing;
    private bool _isSyncInProgress;
    private bool _showAddForm;
    private bool _isEditMode;
    private FeedListItem? _selectedFeed;
    private ObservableCollection<FeedListItem> _feeds = [];
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModel"/> class.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="feedSyncService">The feed synchronization service.</param>
    /// <param name="feedSearchService">The feed search service.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    /// <param name="localNotificationService">The platform notification service, used to detect whether notifications are supported at all.</param>
    public FeedsViewModel(
        IFeedRepository feedRepository,
        ICategoryRepository categoryRepository,
        IFeedSyncService feedSyncService,
        IFeedSearchService feedSearchService,
        INetworkStatusService networkStatusService,
        ILocalNotificationService? localNotificationService = null)
    {
        _feedRepository = feedRepository;
        _categoryRepository = categoryRepository;
        _feedSyncService = feedSyncService;
        _feedSearchService = feedSearchService;
        TrackConnectivity(networkStatusService);
        _localNotificationService = localNotificationService;
        LoadCommand = new AsyncRelayCommand(LoadCommandAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        EditCommand = new AsyncRelayCommand<FeedListItem?>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<FeedListItem?>(DeleteAsync);
        RefreshCommand = new AsyncRelayCommand<FeedListItem?>(RefreshAsync, _ => !IsSyncing);
        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync, () => !IsSyncing);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => IsOnline && !IsSearching && !IsEditMode);
        DirectAddCommand = new AsyncRelayCommand(DirectAddAsync, () => !IsSearching && !IsEditMode);
        SubscribeResultCommand = new AsyncRelayCommand<FeedSearchResult?>(SubscribeResultAsync);
        CloseSearchResultsCommand = new RelayCommand(CloseSearchResults);
        OpenAddFormCommand = new RelayCommand(OpenAddForm);
        CloseAddFormCommand = new RelayCommand(ResetForm);
    }

    /// <summary>
    /// Gets the command that loads the feeds and categories.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that saves the feed currently being edited.
    /// </summary>
    public AsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// Gets the command that prepares an existing feed for editing.
    /// </summary>
    public AsyncRelayCommand<FeedListItem?> EditCommand { get; }

    /// <summary>
    /// Gets the command that deletes a feed.
    /// </summary>
    public AsyncRelayCommand<FeedListItem?> DeleteCommand { get; }

    /// <summary>
    /// Gets the command that refreshes a single feed.
    /// </summary>
    public AsyncRelayCommand<FeedListItem?> RefreshCommand { get; }

    /// <summary>
    /// Gets the command that refreshes all feeds.
    /// </summary>
    public AsyncRelayCommand RefreshAllCommand { get; }

    /// <summary>
    /// Gets the command that opens the add-feed sheet.
    /// </summary>
    public RelayCommand OpenAddFormCommand { get; }

    /// <summary>
    /// Gets the command that closes the add-feed sheet and resets the form.
    /// </summary>
    public RelayCommand CloseAddFormCommand { get; }

    /// <summary>
    /// Gets or sets the URL for a new or edited feed.
    /// </summary>
    public string NewUrl
    {
        get => _newUrl;
        set
        {
            if (SetProperty(ref _newUrl, value))
            {
                SearchResults.Clear();
                ShowSearchResults = false;
                SearchErrorMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets the title for a new or edited feed.
    /// </summary>
    public string NewTitle
    {
        get => _newTitle;
        set => SetProperty(ref _newTitle, value);
    }

    /// <summary>
    /// Gets a value indicating whether the current platform supports local notifications.
    /// When <c>false</c>, the per-feed notification switch is disabled with a platform hint.
    /// </summary>
    public bool NotificationsSupported => _localNotificationService?.IsSupported == true;

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled for the new or edited feed.
    /// </summary>
    public bool FeedNotificationsEnabled
    {
        get => _feedNotificationsEnabled;
        set => SetProperty(ref _feedNotificationsEnabled, value);
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
    /// Gets or sets the current synchronization error message, shown above the feed list.
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
    /// Gets or sets a value indicating whether the add-feed sheet is visible.
    /// </summary>
    public bool ShowAddForm
    {
        get => _showAddForm;
        set => SetProperty(ref _showAddForm, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the sheet edits an existing feed
    /// instead of adding a new one.
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                SearchCommand.NotifyCanExecuteChanged();
                DirectAddCommand.NotifyCanExecuteChanged();
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
                RefreshAllCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the feed currently selected for editing.
    /// </summary>
    public FeedListItem? SelectedFeed
    {
        get => _selectedFeed;
        set => SetProperty(ref _selectedFeed, value);
    }

    /// <summary>
    /// Gets or sets the list of available categories.
    /// </summary>
    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets the list of feeds.
    /// </summary>
    public ObservableCollection<FeedListItem> Feeds
    {
        get => _feeds;
        set => SetProperty(ref _feeds, value);
    }

    private async Task LoadCommandAsync()
    {
        ErrorMessage = string.Empty;
        SyncErrorMessage = string.Empty;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var categories = new List<Category>
        {
            new Category { Id = Guid.Empty, Name = AppResources.CategoryNone },
        };
        categories.AddRange(await _categoryRepository.GetAllAsync());
        Categories = new ObservableCollection<Category>(categories);

        var feeds = await _feedRepository.GetAllWithDetailsAsync();
        Feeds = new ObservableCollection<FeedListItem>(feeds);
    }

    private async Task SaveAsync()
    {
        // Adding feeds runs exclusively through the search, subscribe and
        // direct-add flows; the sheet's save button only exists in edit mode.
        if (SelectedFeed is null)
        {
            return;
        }

        var url = NewUrl.Trim();
        var title = NewTitle.Trim();

        if (string.IsNullOrWhiteSpace(url) || !IsValidFeedUrl(url))
        {
            ErrorMessage = AppResources.ErrorFeedUrlInvalid;
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorMessage = AppResources.ErrorFeedTitleEmpty;
            return;
        }

        var existing = await _feedRepository.GetByUrlAsync(url);
        if (existing is not null && existing.Id != SelectedFeed.Id)
        {
            ErrorMessage = AppResources.ErrorFeedDuplicate;
            return;
        }

        ErrorMessage = string.Empty;

        await _feedRepository.UpdateAsync(ToFeed(
            SelectedFeed,
            url: url,
            title: title,
            categoryId: SelectedFeed.CategoryId,
            notificationsEnabled: FeedNotificationsEnabled));

        ResetForm();
        await LoadAsync();
    }

    private Task EditAsync(FeedListItem? feed)
    {
        if (feed is not null)
        {
            SelectedFeed = feed;
            NewUrl = feed.Url;
            NewTitle = feed.Title;
            FeedNotificationsEnabled = feed.NotificationsEnabled;
            ErrorMessage = string.Empty;
            IsEditMode = true;
            ShowAddForm = true;
        }

        return Task.CompletedTask;
    }

    private async Task DeleteAsync(FeedListItem? feed)
    {
        if (feed is null)
        {
            return;
        }

        await _feedRepository.DeleteAsync(feed.Id);

        if (SelectedFeed?.Id == feed.Id)
        {
            ResetForm();
        }

        await LoadAsync();
    }

    private void ResetForm()
    {
        SelectedFeed = null;
        NewUrl = string.Empty;
        NewTitle = string.Empty;
        FeedNotificationsEnabled = true;
        ShowAddForm = false;
        IsEditMode = false;
        ErrorMessage = string.Empty;
        SearchErrorMessage = string.Empty;
    }

    private async Task RefreshAsync(FeedListItem? feed)
    {
        if (feed is null)
        {
            return;
        }

        var feedId = feed.Id;
        await SyncAsync(() => _feedSyncService.SyncFeedAsync(feedId));
    }

    private async Task RefreshAllAsync()
    {
        await SyncAsync(() => _feedSyncService.SyncAllAsync());
    }

    /// <summary>
    /// Runs the specified feed synchronization exactly once at a time and reloads the list.
    /// The reentrancy guard uses a private runtime flag because <see cref="IsSyncing"/> is
    /// preset to <see langword="true"/> by the <c>RefreshView.IsRefreshing</c> TwoWay binding
    /// before the refresh command executes; guarding on the bound property would turn every
    /// pull-to-refresh gesture into a no-op. The same preset value is reset in the offline
    /// early-return path so the refresh indicator does not hang.
    /// </summary>
    /// <param name="syncAction">The synchronization operation to run.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SyncAsync(Func<Task<SyncResult>> syncAction)
    {
        if (_isSyncInProgress)
        {
            return;
        }

        if (!IsOnline)
        {
            IsSyncing = false;
            return;
        }

        _isSyncInProgress = true;
        IsSyncing = true;
        SyncErrorMessage = string.Empty;

        try
        {
            var result = await syncAction();
            if (result.Status == FeedHealth.Error)
            {
                // The technical detail stays in the SyncLog (persisted via
                // FeedSyncService.UpdateLogAsync); the UI shows the localized
                // generic message instead of raw English/exception text.
                SyncErrorMessage = AppResources.SyncStatusError;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncAsync failed: {ex}");
            SyncErrorMessage = AppResources.SyncStatusError;
        }
        finally
        {
            _isSyncInProgress = false;
            IsSyncing = false;
        }

        await LoadAsync();
    }

    private static bool IsValidFeedUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <inheritdoc />
    protected override void OnConnectivityChanged(bool isOnline)
    {
        SyncErrorMessage = string.Empty;
        SearchErrorMessage = string.Empty;
        SearchCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Renames a feed after confirmation through the rename prompt.
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
        await LoadAsync();
    }

    /// <summary>
    /// Assigns a feed to another category; the pseudo-category entry clears the assignment.
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
        await LoadAsync();
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
        };
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

    private void OpenAddForm()
    {
        // Clear any leftover edit state so add mode always starts from a clean
        // form — an abandoned edit must not leak its URL or selection into it.
        ResetForm();
        ShowAddForm = true;
    }
}
