using Reporter.Core.Models;

namespace Reporter.Core.Services;

public interface IArticleService
{
    Task<IReadOnlyList<Article>> GetUnreadAsync();
    Task<IReadOnlyList<Article>> GetFeedsAsync();
}
