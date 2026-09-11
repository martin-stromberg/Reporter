using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="KeywordMatcher"/> class.
/// </summary>
public class KeywordMatcherTests
{
    private readonly KeywordMatcher _matcher = new();

    /// <summary>
    /// Verifies that MatchesAny matches the title case-insensitively.
    /// </summary>
    [Fact]
    public void MatchesAny_TitleCaseInsensitive()
    {
        var result = _matcher.MatchesAny("Gewinnspiel-Alarm!", null, new[] { "gewinnspiel" });

        Assert.True(result);
    }

    /// <summary>
    /// Verifies that MatchesAny matches the HTML content case-insensitively.
    /// </summary>
    [Fact]
    public void MatchesAny_ContentHtml()
    {
        var result = _matcher.MatchesAny("Normaler Titel", "<p>Jetzt WERBUNG lesen</p>", new[] { "werbung" });

        Assert.True(result);
    }

    /// <summary>
    /// Verifies that MatchesAny matches substrings within words.
    /// </summary>
    [Fact]
    public void MatchesAny_Substring()
    {
        var result = _matcher.MatchesAny("Sponsoringdeal angekündigt", null, new[] { "sponsor" });

        Assert.True(result);
    }

    /// <summary>
    /// Verifies that MatchesAny returns false when no keyword matches title or content.
    /// </summary>
    [Fact]
    public void MatchesAny_NoMatch()
    {
        var result = _matcher.MatchesAny("Tech-News", "<p>Inhalt</p>", new[] { "werbung" });

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that MatchesAny returns false when title and content are null.
    /// </summary>
    [Fact]
    public void MatchesAny_NullTitleAndContent_ReturnsFalse()
    {
        var result = _matcher.MatchesAny(null, null, new[] { "werbung" });

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that MatchesAny returns false for an empty keyword list.
    /// </summary>
    [Fact]
    public void MatchesAny_EmptyKeywords_ReturnsFalse()
    {
        var result = _matcher.MatchesAny("Werbung", "Werbung", Array.Empty<string>());

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that MatchesAny skips blank keywords.
    /// </summary>
    [Fact]
    public void MatchesAny_BlankKeywords_Skipped()
    {
        var result = _matcher.MatchesAny("Artikel", "Inhalt", new[] { " ", string.Empty });

        Assert.False(result);
    }
}
