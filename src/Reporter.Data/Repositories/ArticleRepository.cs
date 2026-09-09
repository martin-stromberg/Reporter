using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Data.Repositories;

public class ArticleRepository : IArticleRepository
{
    public Task<IReadOnlyList<Article>> GetArticlesAsync() =>
        Task.FromResult<IReadOnlyList<Article>>(new List<Article>());

    public Task<IReadOnlyList<Article>> GetUnreadAsync() =>
        Task.FromResult<IReadOnlyList<Article>>(new List<Article>());

    public Task SaveAsync(Article article) => Task.CompletedTask;
}
