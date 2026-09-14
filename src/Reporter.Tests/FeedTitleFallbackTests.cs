// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="FeedTitleFallback"/> helper.
/// </summary>
public class FeedTitleFallbackTests
{
    /// <summary>
    /// Verifies that the fallback title is the last path segment of the URL.
    /// </summary>
    [Fact]
    public void GetFallbackTitle_ReturnsLastPathSegment()
    {
        var title = FeedTitleFallback.GetFallbackTitle("https://heise.de/rss/heise-atom.xml");

        Assert.Equal("heise-atom.xml", title);
    }

    /// <summary>
    /// Verifies that URL-encoded path segments are decoded.
    /// </summary>
    [Fact]
    public void GetFallbackTitle_DecodesUrlEncodedSegment()
    {
        var title = FeedTitleFallback.GetFallbackTitle("https://example.com/feeds/my%20feed.xml");

        Assert.Equal("my feed.xml", title);
    }

    /// <summary>
    /// Verifies that a URL without a path falls back to the host name.
    /// </summary>
    [Fact]
    public void GetFallbackTitle_FallsBackToHost_WhenNoPath()
    {
        var title = FeedTitleFallback.GetFallbackTitle("https://heise.de/");

        Assert.Equal("heise.de", title);
    }

    /// <summary>
    /// Verifies that an unparseable input falls back to the input itself.
    /// </summary>
    [Fact]
    public void GetFallbackTitle_FallsBackToUrl_WhenUnparseable()
    {
        var title = FeedTitleFallback.GetFallbackTitle("not a url");

        Assert.Equal("not a url", title);
    }

    /// <summary>
    /// Verifies that a trailing slash is ignored when picking the last segment.
    /// </summary>
    [Fact]
    public void GetFallbackTitle_TrailingSlash_UsesLastNonEmptySegment()
    {
        var title = FeedTitleFallback.GetFallbackTitle("https://heise.de/rss/heise-atom.xml/");

        Assert.Equal("heise-atom.xml", title);
    }

    /// <summary>
    /// Verifies that a title equal to the URL file name counts as a placeholder.
    /// </summary>
    [Fact]
    public void IsFileNamePlaceholderTitle_WhenTitleMatchesFileName_ReturnsTrue()
    {
        var result = FeedTitleFallback.IsFileNamePlaceholderTitle("heise-atom.xml", "https://heise.de/rss/heise-atom.xml");

        Assert.True(result);
    }

    /// <summary>
    /// Verifies that a title different from the URL file name is no placeholder.
    /// </summary>
    [Fact]
    public void IsFileNamePlaceholderTitle_WhenTitleDiffers_ReturnsFalse()
    {
        var result = FeedTitleFallback.IsFileNamePlaceholderTitle("Test Feed", "https://example.com/rss");

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that the file-name comparison ignores casing.
    /// </summary>
    [Fact]
    public void IsFileNamePlaceholderTitle_MatchesCaseInsensitive()
    {
        var result = FeedTitleFallback.IsFileNamePlaceholderTitle("HEISE-ATOM.XML", "https://heise.de/rss/heise-atom.xml");

        Assert.True(result);
    }
}
