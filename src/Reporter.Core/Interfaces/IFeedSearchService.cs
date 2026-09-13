// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Searches for feeds that belong to an entered domain or URL.
/// </summary>
public interface IFeedSearchService
{
    /// <summary>
    /// Searches the feed directory and the entered website for feed documents.
    /// </summary>
    /// <param name="query">The absolute feed/site URL or normalized website URL to search for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// ordered list of feed search results.
    /// </returns>
    /// <exception cref="FeedSearchUnavailableException">
    /// Thrown when both the feed directory and the autodiscovery are unreachable.
    /// </exception>
    Task<IReadOnlyList<FeedSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
