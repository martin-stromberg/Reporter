using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Provides application services for retrieving articles.
/// </summary>
public class ArticleService : IArticleService
{
    private readonly IArticleRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleService"/> class.
    /// </summary>
    /// <param name="repository">The article repository.</param>
    public ArticleService(IArticleRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets unread articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread articles.</returns>
    public Task<IReadOnlyList<Article>> GetUnreadAsync() => _repository.GetUnreadAsync();

    /// <summary>
    /// Gets all feed articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the feed articles.</returns>
    public async Task<IReadOnlyList<Article>> GetFeedsAsync()
    {
        var all = await _repository.GetArticlesAsync();
        return all;
    }
}
