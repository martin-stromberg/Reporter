using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

/// <summary>
/// View model for managing feeds.
/// </summary>
public partial class FeedsViewModel : BaseViewModel
{
    private readonly IFeedRepository _feedRepository;
    private readonly ICategoryRepository _categoryRepository;

    private string _newUrl = string.Empty;
    private string _newTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private FeedListItem? _selectedFeed;
    private Category? _selectedCategory;
    private ObservableCollection<FeedListItem> _feeds = [];
    private ObservableCollection<Category> _categories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModel"/> class.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    public FeedsViewModel(IFeedRepository feedRepository, ICategoryRepository categoryRepository)
    {
        _feedRepository = feedRepository;
        _categoryRepository = categoryRepository;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        EditCommand = new AsyncRelayCommand<FeedListItem?>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<FeedListItem?>(DeleteAsync);
    }

    /// <summary>
    /// Gets the command that loads the feeds and categories.
    /// </summary>
    public AsyncRelayCommand LoadCommand { get; }

    /// <summary>
    /// Gets the command that saves a new or existing feed.
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
    /// Gets or sets the URL for a new or edited feed.
    /// </summary>
    public string NewUrl
    {
        get => _newUrl;
        set => SetProperty(ref _newUrl, value);
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
    /// Gets or sets the feed currently selected for editing.
    /// </summary>
    public FeedListItem? SelectedFeed
    {
        get => _selectedFeed;
        set => SetProperty(ref _selectedFeed, value);
    }

    /// <summary>
    /// Gets or sets the category selected for a new or edited feed.
    /// </summary>
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
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

    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;

        var categories = new List<Category>
        {
            new Category { Id = Guid.Empty, Name = AppResources.CategoryNone },
        };
        categories.AddRange(await _categoryRepository.GetAllAsync());
        Categories = new ObservableCollection<Category>(categories);

        var feeds = await _feedRepository.GetAllWithDetailsAsync();
        Feeds = new ObservableCollection<FeedListItem>(feeds);

        if (SelectedCategory is null || Categories.All(c => c.Id != SelectedCategory.Id))
        {
            SelectedCategory = Categories.FirstOrDefault();
        }
    }

    private async Task SaveAsync()
    {
        var url = NewUrl.Trim();
        var title = NewTitle.Trim();

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
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
        if (existing is not null && (SelectedFeed is null || existing.Id != SelectedFeed.Id))
        {
            ErrorMessage = AppResources.ErrorFeedDuplicate;
            return;
        }

        ErrorMessage = string.Empty;

        var categoryId = SelectedCategory?.Id == Guid.Empty ? null : SelectedCategory?.Id;

        if (SelectedFeed is null)
        {
            await _feedRepository.AddAsync(new Feed
            {
                Id = Guid.NewGuid(),
                Url = url,
                Title = title,
                CategoryId = categoryId,
                LastCheckedAt = null,
                HealthStatus = "OK",
                HealthLastChange = null,
            });
        }
        else
        {
            await _feedRepository.UpdateAsync(new Feed
            {
                Id = SelectedFeed.Id,
                Url = url,
                Title = title,
                CategoryId = categoryId,
                LastCheckedAt = SelectedFeed.LastCheckedAt,
                HealthStatus = SelectedFeed.HealthStatus,
                HealthLastChange = SelectedFeed.HealthLastChange,
            });
        }

        NewUrl = string.Empty;
        NewTitle = string.Empty;
        SelectedFeed = null;
        SelectedCategory = Categories.FirstOrDefault();
        await LoadAsync();
    }

    private Task EditAsync(FeedListItem? feed)
    {
        if (feed is not null)
        {
            SelectedFeed = feed;
            NewUrl = feed.Url;
            NewTitle = feed.Title;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == (feed.CategoryId ?? Guid.Empty));
            ErrorMessage = string.Empty;
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
            SelectedFeed = null;
            NewUrl = string.Empty;
            NewTitle = string.Empty;
            SelectedCategory = Categories.FirstOrDefault();
        }

        await LoadAsync();
    }
}
