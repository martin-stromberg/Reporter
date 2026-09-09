using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Data.Repositories;

/// <summary>
/// In-memory implementation of the <see cref="IArticleRepository"/> interface.
/// </summary>
public class ArticleRepository : IArticleRepository
{
    /// <summary>
    /// Gets all articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains all articles.</returns>
    public Task<IReadOnlyList<Article>> GetArticlesAsync() =>
        Task.FromResult<IReadOnlyList<Article>>(new List<Article>());

    /// <summary>
    /// Gets unread articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains unread articles.</returns>
    public Task<IReadOnlyList<Article>> GetUnreadAsync() =>
        Task.FromResult<IReadOnlyList<Article>>(new List<Article>());

    /// <summary>
    /// Saves the specified article asynchronously.
    /// </summary>
    /// <param name="article">The article to save.</param>
    public Task SaveAsync(Article article) => Task.CompletedTask;
}
