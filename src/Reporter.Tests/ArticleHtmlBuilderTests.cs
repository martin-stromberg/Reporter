// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ArticleHtmlBuilder"/> class.
/// </summary>
public class ArticleHtmlBuilderTests
{
    /// <summary>
    /// Verifies that a non-empty tag list produces the tags CSS rule and a
    /// <c>&lt;p class='tags'&gt;</c> block after the content inside the body
    /// with the terms joined comma-separated.
    /// </summary>
    [Fact]
    public void Build_WithTags_AppendsTagBlock()
    {
        var html = ArticleHtmlBuilder.Build("<p>Article text</p>", new[] { "SWB", "Stadtbahnen" }, 19);

        Assert.Contains("p.tags", html);
        Assert.Contains("<p class='tags'>SWB, Stadtbahnen</p>", html);
        var contentIndex = html.IndexOf("<p>Article text</p>", StringComparison.Ordinal);
        var tagIndex = html.IndexOf("<p class='tags'>", StringComparison.Ordinal);
        var bodyEndIndex = html.IndexOf("</body>", StringComparison.Ordinal);
        Assert.True(contentIndex >= 0 && tagIndex > contentIndex && tagIndex < bodyEndIndex);
    }

    /// <summary>
    /// Verifies that tag terms carrying HTML markup are encoded so they cannot
    /// inject elements or attributes into the document — the block is appended
    /// after sanitization.
    /// </summary>
    [Fact]
    public void Build_Tags_AreHtmlEncoded()
    {
        var html = ArticleHtmlBuilder.Build("<p>text</p>", new[] { "<script>alert(1)</script>", "a\"onerror=\"x" }, 19);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&quot;", html);
    }

    /// <summary>
    /// Verifies that an empty tag list renders no tag block at all.
    /// </summary>
    [Fact]
    public void Build_WithoutTags_NoTagBlock()
    {
        var html = ArticleHtmlBuilder.Build("<p>Article text</p>", [], 19);

        Assert.DoesNotContain("<p class='tags'>", html);
        Assert.Contains("<body><p>Article text</p></body>", html);
    }
}
