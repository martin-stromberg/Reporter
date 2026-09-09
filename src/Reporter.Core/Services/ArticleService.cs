using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

public class ArticleService(IArticleRepository repository) : IArticleService
{
    public Task<IReadOnlyList<Article>> GetUnreadAsync() => repository.GetUnreadAsync();

    public async Task<IReadOnlyList<Article>> GetFeedsAsync()
    {
        var all = await repository.GetArticlesAsync();
        return all;
    }
}
