// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="WebViewNavigationGuard"/> class.
/// </summary>
public class WebViewNavigationGuardTests
{
    /// <summary>
    /// Verifies that http and https URLs are classified as external navigations.
    /// </summary>
    /// <param name="url">The URL to classify.</param>
    [Theory]
    [InlineData("http://example.com/article")]
    [InlineData("https://example.com/article")]
    [InlineData("HTTPS://EXAMPLE.COM/ARTICLE")]
    public void IsExternalUrl_WebUrls_ReturnsTrue(string url)
    {
        Assert.True(WebViewNavigationGuard.IsExternalUrl(url));
    }

    /// <summary>
    /// Verifies that local content loads and non-web URLs are not classified
    /// as external navigations.
    /// </summary>
    /// <param name="url">The URL to classify.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("about:blank")]
    [InlineData("data:text/html;base64,PGI+aGk8L2I+")]
    [InlineData("file:///local/article.html")]
    [InlineData("ftp://example.com/file")]
    public void IsExternalUrl_LocalOrOtherUrls_ReturnsFalse(string? url)
    {
        Assert.False(WebViewNavigationGuard.IsExternalUrl(url));
    }
}
