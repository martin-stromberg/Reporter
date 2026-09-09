using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Resources.Strings;

namespace Reporter.ViewModels;

public partial class UnreadViewModel : BaseViewModel
{
    private readonly IArticleService _articleService;
    private string _title = AppResources.PageTitleUnread;
    private IReadOnlyList<Article> _articles = new List<Article>();

    public UnreadViewModel(IArticleService articleService)
    {
        _articleService = articleService;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public IReadOnlyList<Article> Articles
    {
        get => _articles;
        set => SetProperty(ref _articles, value);
    }
}
