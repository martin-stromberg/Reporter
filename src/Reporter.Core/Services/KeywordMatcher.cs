// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Matches item content against the configured keyword blacklist using
/// case-insensitive substring comparison on title and HTML content.
/// </summary>
public class KeywordMatcher : IKeywordMatcher
{
    /// <inheritdoc />
    public bool MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (title is not null && title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (contentHtml is not null && contentHtml.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
