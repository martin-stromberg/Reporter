// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for managing feeds.
/// </summary>
public partial class FeedsViewModel : BaseViewModel
{
    private readonly IFeedRepository _feedRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFeedSyncService _feedSyncService;
    private readonly IFeedIconService _feedIconService;

    private string _newUrl = string.Empty;
    private string _errorMessage = string.Empty;
    private string _syncErrorMessage = string.Empty;
    private bool _isSyncing;
    private bool _showAddForm;
    private ObservableCollection<FeedListItem> _feeds = [];
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModel"/> class.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="feedSyncService">The feed synchronization service.</param>
    /// <param name="feedSearchService">The feed search service.</param>
    /// <param name="feedIconService">The service used to discover the favicon of a feed's website.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    public FeedsViewModel(
        IFeedRepository feedRepository,
        ICategoryRepository categoryRepository,
        IFeedSyncService feedSyncService,
        IFeedSearchService feedSearchService,
        IFeedIconService feedIconService,
        INetworkStatusService networkStatusService)
    {
        _feedRepository = feedRepository;
        _categoryRepository = categoryRepository;
        _feedSyncService = feedSyncService;
        _feedSearchService = feedSearchService;
        _feedIconService = feedIconService;
        TrackConnectivity(networkStatusService);
        LoadCommand = new AsyncRelayCommand(LoadCommandAsync);
        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync, () => !IsSyncing);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => IsOnline && !IsSearching);
        DirectAddCommand = new AsyncRelayCommand(DirectAddAsync, () => !IsSearching);
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
    /// Gets or sets a value indicating whether a synchronization is in progress.
    /// </summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        set
        {
            if (SetProperty(ref _isSyncing, value))
            {
                RefreshAllCommand.NotifyCanExecuteChanged();
            }
        }
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
        try
        {
            Categories = await LoadCategoriesWithNoneAsync(_categoryRepository);

            var feeds = await _feedRepository.GetAllWithDetailsAsync();
            Feeds = new ObservableCollection<FeedListItem>(feeds);
        }
        catch (Exception ex)
        {
            // A repository failure must not escape as an unobserved faulted
            // command task — it is logged and surfaced on the page's error
            // label while the list keeps its last loaded state.
            Debug.WriteLine($"LoadAsync failed: {ex}");
            ErrorMessage = AppResources.ErrorLoadFailed;
        }
    }

    private void ResetForm()
    {
        NewUrl = string.Empty;
        ShowAddForm = false;
        ErrorMessage = string.Empty;
        SearchErrorMessage = string.Empty;
    }

    private async Task RefreshAllAsync()
    {
        await SyncAsync(() => _feedSyncService.SyncAllAsync());
    }

    /// <summary>
    /// Runs the specified feed synchronization exactly once at a time and reloads the list.
    /// </summary>
    /// <param name="syncAction">The synchronization operation to run.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SyncAsync(Func<Task<SyncResult>> syncAction)
    {
        if (await RunFeedSyncAsync(syncAction))
        {
            await LoadAsync();
        }
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

    /// <inheritdoc />
    protected override void OnConnectivityChanged(bool isOnline)
    {
        SyncErrorMessage = string.Empty;
        SearchErrorMessage = string.Empty;
        SearchCommand.NotifyCanExecuteChanged();
    }

    private void OpenAddForm()
    {
        // Clear any leftover edit state so add mode always starts from a clean
        // form — an abandoned edit must not leak its URL or selection into it.
        ResetForm();
        ShowAddForm = true;
    }
}
