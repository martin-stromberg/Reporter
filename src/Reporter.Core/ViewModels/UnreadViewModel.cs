using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// View model for the unread articles page.
/// </summary>
public partial class UnreadViewModel : BaseViewModel
{
    private readonly IItemRepository _itemRepository;
    private string _title = AppResources.PageTitleUnread;
    private IReadOnlyList<Item> _articles = new List<Item>();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModel"/> class.
    /// </summary>
    /// <param name="itemRepository">The item repository.</param>
    public UnreadViewModel(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the list of unread items.
    /// </summary>
    public IReadOnlyList<Item> Articles
    {
        get => _articles;
        set => SetProperty(ref _articles, value);
    }
}
