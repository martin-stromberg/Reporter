using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

/// <summary>
/// View model for the feeds page.
/// </summary>
public partial class FeedsViewModel : BaseViewModel
{
    private readonly IArticleService _articleService;
    private string _title = AppResources.PageTitleFeeds;
    private IReadOnlyList<Article> _articles = new List<Article>();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedsViewModel"/> class.
    /// </summary>
    /// <param name="articleService">The article service.</param>
    public FeedsViewModel(IArticleService articleService)
    {
        _articleService = articleService;
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
    /// Gets or sets the list of feed articles.
    /// </summary>
    public IReadOnlyList<Article> Articles
    {
        get => _articles;
        set => SetProperty(ref _articles, value);
    }
}
