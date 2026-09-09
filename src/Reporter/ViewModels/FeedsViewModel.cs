using CommunityToolkit.Mvvm.ComponentModel;
using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.ViewModels;

public partial class FeedsViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _title = "Feeds";

    [ObservableProperty]
    private IReadOnlyList<Article> _articles = new List<Article>();

    public FeedsViewModel(IArticleService articleService) { }
}
