// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Loads the configured keyword filter list once per operation via
/// <see cref="IKeywordRepository"/> and matches item content against it via
/// <see cref="IKeywordMatcher"/>.
/// </summary>
public class KeywordFilter : IKeywordFilter
{
    private readonly IKeywordRepository _keywordRepository;
    private readonly IKeywordMatcher _keywordMatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordFilter"/> class.
    /// </summary>
    /// <param name="keywordRepository">The keyword repository providing the configured filter list.</param>
    /// <param name="keywordMatcher">The matcher used to filter items against the keywords.</param>
    public KeywordFilter(IKeywordRepository keywordRepository, IKeywordMatcher keywordMatcher)
    {
        _keywordRepository = keywordRepository;
        _keywordMatcher = keywordMatcher;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetKeywordTextsAsync(Guid? feedId)
    {
        var keywords = feedId.HasValue
            ? await _keywordRepository.GetEffectiveForFeedAsync(feedId.Value).ConfigureAwait(false)
            : await _keywordRepository.GetByFeedAsync(null).ConfigureAwait(false);
        return keywords.Select(k => k.KeywordText).ToList();
    }

    /// <inheritdoc />
    public Task<bool> HasKeywordsAsync()
    {
        return _keywordRepository.AnyAsync();
    }

    /// <inheritdoc />
    public bool MatchesAny(string? title, string? contentHtml, IReadOnlyList<string> keywordTexts)
    {
        return keywordTexts.Count > 0 && _keywordMatcher.MatchesAny(title, contentHtml, keywordTexts);
    }
}
