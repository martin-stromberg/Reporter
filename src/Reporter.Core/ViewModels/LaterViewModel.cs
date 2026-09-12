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
    private readonly IItemRepository _itemRepository;
    private string _title = AppResources.PageTitleLater;
    private IReadOnlyList<ItemListItem> _savedItems = new List<ItemListItem>();

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
        ToggleSavedCommand = new AsyncRelayCommand<ItemListItem?>(ToggleSavedAsync);
        MarkReadCommand = new AsyncRelayCommand<ItemListItem?>(MarkReadAsync);
    }

    /// <summary>
    /// Gets the command that loads saved-for-later articles.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

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
    /// Gets or sets the list of items saved for later.
    /// </summary>
    public IReadOnlyList<ItemListItem> SavedItems
    {
        get => _savedItems;
        set => SetProperty(ref _savedItems, value);
    }

    private async Task LoadAsync()
    {
        SavedItems = await _itemRepository.GetSavedForLaterAsync();
    }

    private async Task ToggleSavedAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.ToggleSavedForLaterAsync(item.Id);
        await LoadAsync();
    }

    private async Task MarkReadAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _itemRepository.MarkAsReadAsync(item.Id);
        await LoadAsync();
    }
}
