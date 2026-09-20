// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Loads the configured keyword filter list and matches item content against it.
/// </summary>
public interface IKeywordFilter
{
    /// <summary>
    /// Gets the keyword texts effective for the specified feed scope asynchronously.
    /// </summary>
    /// <param name="feedId">The feed identifier, or <c>null</c> for the global keyword list only.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of keyword texts; for a feed it is the union of global and feed keywords.</returns>
    Task<IReadOnlyList<string>> GetKeywordTextsAsync(Guid? feedId);

    /// <summary>
    /// Checks whether at least one keyword is configured, in any scope (global or feed-scoped).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when any keyword exists.</returns>
    Task<bool> HasKeywordsAsync();

    /// <summary>
    /// Determines whether the title or the HTML content matches any of the specified keyword texts.
    /// The comparison is case-insensitive and matches substrings.
    /// </summary>
    /// <param name="title">The item title.</param>
    /// <param name="contentHtml">The item HTML content.</param>
    /// <param name="keywordTexts">The keyword texts to match.</param>
    /// <returns><c>true</c> if any keyword matches; otherwise <c>false</c>.</returns>
    bool MatchesAny(string? title, string? contentHtml, IReadOnlyList<string> keywordTexts);
}
