// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private string _title = AppResources.PageTitleLater;
    private bool _isLoading;
    private bool _hasMore = true;
    private int _currentPage;
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    public LaterViewModel(IItemRepository itemRepository, INetworkStatusService networkStatusService)
    {
        _itemRepository = itemRepository;
        TrackConnectivity(networkStatusService);
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
            SavedItems.Clear();
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
            var items = await _itemRepository.GetSavedForLaterAsync(_currentPage, PageSize);
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
        SavedItems.Remove(item);
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);

        var index = SavedItems.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        SavedItems[index] = item.CopyWith(isRead: true);
    }
}
