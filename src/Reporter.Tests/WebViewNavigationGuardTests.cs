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

    /// <summary>
    /// Verifies that an external web URL is redirected to the system browser
    /// when the device is online.
    /// </summary>
    /// <param name="url">The external URL to decide on.</param>
    [Theory]
    [InlineData("http://example.com/article")]
    [InlineData("https://example.com/article")]
    public void DecideAction_ExternalUrl_Online_ReturnsCancelAndOpenExternally(string url)
    {
        Assert.Equal(
            WebViewNavigationAction.CancelAndOpenExternally,
            WebViewNavigationGuard.DecideAction(url, isOnline: true));
    }

    /// <summary>
    /// Verifies that an external web URL is cancelled with the offline hint
    /// when the device is offline.
    /// </summary>
    /// <param name="url">The external URL to decide on.</param>
    [Theory]
    [InlineData("http://example.com/article")]
    [InlineData("https://example.com/article")]
    public void DecideAction_ExternalUrl_Offline_ReturnsCancelAndShowOfflineHint(string url)
    {
        Assert.Equal(
            WebViewNavigationAction.CancelAndShowOfflineHint,
            WebViewNavigationGuard.DecideAction(url, isOnline: false));
    }

    /// <summary>
    /// Verifies that local content loads and non-web URLs pass through
    /// regardless of the connectivity state.
    /// </summary>
    /// <param name="url">The URL to decide on.</param>
    /// <param name="isOnline">The connectivity state.</param>
    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("", true)]
    [InlineData("", false)]
    [InlineData("about:blank", true)]
    [InlineData("about:blank", false)]
    [InlineData("data:text/html;base64,PGI+aGk8L2I+", true)]
    [InlineData("data:text/html;base64,PGI+aGk8L2I+", false)]
    [InlineData("file:///local/article.html", true)]
    [InlineData("file:///local/article.html", false)]
    [InlineData("ftp://example.com/file", true)]
    [InlineData("ftp://example.com/file", false)]
    public void DecideAction_LocalOrOtherUrl_ReturnsProceed(string? url, bool isOnline)
    {
        Assert.Equal(
            WebViewNavigationAction.Proceed,
            WebViewNavigationGuard.DecideAction(url, isOnline));
    }
}
