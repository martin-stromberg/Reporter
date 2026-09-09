using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Defines read and write access to <see cref="Article"/> entities.
/// </summary>
public interface IArticleRepository
{
    /// <summary>
    /// Gets all articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of articles.</returns>
    Task<IReadOnlyList<Article>> GetArticlesAsync();

    /// <summary>
    /// Gets all unread articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread articles.</returns>
    Task<IReadOnlyList<Article>> GetUnreadAsync();

    /// <summary>
    /// Saves the specified article asynchronously.
    /// </summary>
    /// <param name="article">The article to save.</param>
    Task SaveAsync(Article article);
}
