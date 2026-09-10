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
    private IReadOnlyList<Item> _savedItems = new List<Item>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LaterViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    public LaterViewModel(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>
    /// Gets the command that loads saved-for-later articles.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

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
    public IReadOnlyList<Item> SavedItems
    {
        get => _savedItems;
        set => SetProperty(ref _savedItems, value);
    }

    private async Task LoadAsync()
    {
        SavedItems = await _itemRepository.GetSavedForLaterAsync();
    }
}
