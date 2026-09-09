using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Defines application-level operations for reading articles.
/// </summary>
public interface IArticleService
{
    /// <summary>
    /// Gets unread articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unread articles.</returns>
    Task<IReadOnlyList<Article>> GetUnreadAsync();

    /// <summary>
    /// Gets all feed articles asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the feed articles.</returns>
    Task<IReadOnlyList<Article>> GetFeedsAsync();
}
