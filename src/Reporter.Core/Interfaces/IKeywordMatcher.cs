// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Matches item content against the configured keyword blacklist.
/// </summary>
public interface IKeywordMatcher
{
    /// <summary>
    /// Determines whether the title or the HTML content contains any of the specified keywords.
    /// The comparison is case-insensitive and matches substrings.
    /// </summary>
    /// <param name="title">The item title.</param>
    /// <param name="contentHtml">The item HTML content.</param>
    /// <param name="keywords">The keywords to match.</param>
    /// <returns><c>true</c> if any keyword matches; otherwise <c>false</c>.</returns>
    bool MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords);
}
