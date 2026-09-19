// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text.RegularExpressions;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ArticleHtmlSanitizer"/> class.
/// </summary>
public class ArticleHtmlSanitizerTests
{
    /// <summary>
    /// Verifies that dangerous tags and inline event handlers are removed in both modes.
    /// </summary>
    /// <param name="forOffline">Whether the sanitizer runs in offline mode.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sanitize_RemovesScriptsAndEventHandlers(bool forOffline)
    {
        var html = "<p onclick=\"alert(1)\">Text<script>alert('x')</script></p>" +
            "<iframe src=\"https://evil.example\"></iframe>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline);

        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert('x')", result);
        Assert.DoesNotContain("<iframe", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Text", result);
    }

    /// <summary>
    /// Verifies that anchors and images are preserved in online mode.
    /// </summary>
    [Fact]
    public void Sanitize_Online_PreservesAnchorsAndImages()
    {
        var html = "<p><a href=\"https://example.com\">Link</a>" +
            "<img src=\"https://example.com/img.png\" alt=\"Bild\"/></p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: false);

        Assert.Contains("<a href=\"https://example.com\">Link</a>", result);
        Assert.Contains("<img", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that offline mode neutralizes anchors but keeps their inner text.
    /// </summary>
    [Fact]
    public void Sanitize_Offline_NeutralizesAnchorsButKeepsText()
    {
        var html = "<p>Vor <a href=\"https://example.com\" target=\"_blank\">dem Link</a> nach.</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: true);

        Assert.DoesNotContain("<a ", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</a>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vor dem Link nach.", result);
    }

    /// <summary>
    /// Verifies that offline mode removes complete img elements including arbitrary attributes.
    /// </summary>
    [Fact]
    public void Sanitize_Offline_RemovesImages()
    {
        var html = "<p>A</p><img src=\"https://example.com/a.png\" class=\"wide\" alt=\"x\"/>" +
            "<img src='https://example.com/b.png' width='100'><p>B</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: true);

        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>A</p>", result);
        Assert.Contains("<p>B</p>", result);
    }

    /// <summary>
    /// Verifies that applet, audio and video elements are removed completely
    /// including their content and closing tags.
    /// </summary>
    /// <param name="forOffline">Whether the sanitizer runs in offline mode.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sanitize_RemovesDangerousElementsIncludingClosingTags(bool forOffline)
    {
        var html = "<p>A</p><audio src=\"https://example.com/a.mp3\"><source src=\"x\"/></audio>" +
            "<video width=\"100\">fallback</video><applet code=\"x\"></applet><p>B</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline);

        Assert.DoesNotContain("<audio", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</audio", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<video", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</video", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<applet", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</applet", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fallback", result);
        Assert.Contains("<p>A</p>", result);
        Assert.Contains("<p>B</p>", result);
    }

    /// <summary>
    /// Verifies that orphaned closing tags of dangerous elements without a
    /// matching opening tag are removed as well.
    /// </summary>
    /// <param name="forOffline">Whether the sanitizer runs in offline mode.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sanitize_RemovesOrphanedDangerousClosingTags(bool forOffline)
    {
        var html = "<p>A</p></audio></video ></applet><p>B</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline);

        Assert.DoesNotContain("</audio", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</video", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</applet", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>A</p>", result);
        Assert.Contains("<p>B</p>", result);
    }

    /// <summary>
    /// Verifies that null and whitespace input is returned unchanged.
    /// </summary>
    /// <param name="input">The input to sanitize.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_NullOrWhitespace_ReturnsInput(string? input)
    {
        Assert.Equal(input, ArticleHtmlSanitizer.Sanitize(input, forOffline: false));
        Assert.Equal(input, ArticleHtmlSanitizer.Sanitize(input, forOffline: true));
    }

    /// <summary>
    /// Verifies that offline mode with a local image replaces the first
    /// <c>&lt;img&gt;</c> tag by a <c>data:</c> URI image and removes the
    /// remaining image tags.
    /// </summary>
    [Fact]
    public void Sanitize_Offline_WithLocalImage_ReplacesFirstImgWithDataUri()
    {
        var image = new Reporter.Core.Models.ItemImage([1, 2, 3], "image/png", null);
        var html = "<p>A</p><img src=\"https://example.com/a.png\" />" +
            "<img src=\"https://example.com/b.png\" /><p>B</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: true, localImage: image);

        var expectedTag = $"<img src=\"data:image/png;base64,{Convert.ToBase64String(image.Data)}\" />";
        Assert.Contains(expectedTag, result);
        Assert.DoesNotContain("example.com", result);
        Assert.NotNull(result);
        Assert.Single(Regex.Matches(result, "<img"));
        Assert.Contains("<p>A</p>", result);
        Assert.Contains("<p>B</p>", result);
    }

    /// <summary>
    /// Verifies that offline mode with a local image prepends a header image
    /// when the content holds no <c>&lt;img&gt;</c> tag.
    /// </summary>
    [Fact]
    public void Sanitize_Offline_WithLocalImageAndNoImg_PrependsHeaderImage()
    {
        var image = new Reporter.Core.Models.ItemImage([9], "image/jpeg", null);
        var html = "<p>Text only</p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: true, localImage: image);

        Assert.StartsWith($"<img src=\"data:image/jpeg;base64,{Convert.ToBase64String(image.Data)}\" />", result);
        Assert.Contains("<p>Text only</p>", result);
    }

    /// <summary>
    /// Verifies that offline mode with a local image and an empty or
    /// <c>null</c> content returns a fragment containing only the header image.
    /// </summary>
    /// <param name="input">The input to sanitize.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_Offline_EmptyContentWithLocalImage_ReturnsHeaderImage(string? input)
    {
        var image = new Reporter.Core.Models.ItemImage([5], "image/png", null);

        var result = ArticleHtmlSanitizer.Sanitize(input, forOffline: true, localImage: image);

        Assert.Equal($"<img src=\"data:image/png;base64,{Convert.ToBase64String(image.Data)}\" />", result);
    }

    /// <summary>
    /// Verifies that a supplied local image is ignored in online mode so remote
    /// images stay untouched.
    /// </summary>
    [Fact]
    public void Sanitize_Online_WithLocalImage_KeepsRemoteImages()
    {
        var image = new Reporter.Core.Models.ItemImage([1], "image/png", null);
        var html = "<p><img src=\"https://example.com/a.png\" /></p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: false, localImage: image);

        Assert.Contains("src=\"https://example.com/a.png\"", result);
        Assert.DoesNotContain("data:image", result);
    }

    /// <summary>
    /// Verifies that offline mode without a local image still removes all
    /// image tags.
    /// </summary>
    [Fact]
    public void Sanitize_Offline_WithoutLocalImage_RemovesAllImages()
    {
        var html = "<p><img src=\"https://example.com/a.png\" /><img src=\"https://example.com/b.png\" /></p>";

        var result = ArticleHtmlSanitizer.Sanitize(html, forOffline: true, localImage: null);

        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
    }
}
