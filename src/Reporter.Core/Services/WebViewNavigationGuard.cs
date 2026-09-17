// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Classifies WebView navigation targets so that only real external link
/// navigations can be blocked while local content loads pass through.
/// </summary>
public static class WebViewNavigationGuard
{
    /// <summary>
    /// Determines whether the given URL targets an external web resource.
    /// Local content loads (<c>about:blank</c>, <c>data:</c>, <c>file:</c>,
    /// platform-specific schemes) and empty URLs are not external.
    /// </summary>
    /// <param name="url">The navigation URL reported by the WebView.</param>
    /// <returns><c>true</c> when the URL uses the http or https scheme; otherwise <c>false</c>.</returns>
    public static bool IsExternalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Decides how the WebView should handle a navigation request: local
    /// content loads proceed, external links are cancelled — online they are
    /// handed to the system browser, offline the hint dialog is shown.
    /// </summary>
    /// <param name="url">The navigation URL reported by the WebView.</param>
    /// <param name="isOnline">The current connectivity state.</param>
    /// <returns>The <see cref="WebViewNavigationAction"/> for the request.</returns>
    public static WebViewNavigationAction DecideAction(string? url, bool isOnline)
    {
        if (!IsExternalUrl(url))
        {
            return WebViewNavigationAction.Proceed;
        }

        return isOnline
            ? WebViewNavigationAction.CancelAndOpenExternally
            : WebViewNavigationAction.CancelAndShowOfflineHint;
    }
}
