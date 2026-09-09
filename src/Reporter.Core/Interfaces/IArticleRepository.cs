using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetArticlesAsync();
    Task<IReadOnlyList<Article>> GetUnreadAsync();
    Task SaveAsync(Article article);
}
