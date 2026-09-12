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
}
