using CommunityToolkit.Mvvm.ComponentModel;
using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.ViewModels;

public partial class UngelesenViewModel : BaseViewModel
{
    private readonly IArticleService _articleService;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private IReadOnlyList<Article> _articles;

    public UngelesenViewModel(IArticleService articleService)
    {
        _articleService = articleService;
        _title = "Ungelesen";
        _articles = new List<Article>();
    }
}
