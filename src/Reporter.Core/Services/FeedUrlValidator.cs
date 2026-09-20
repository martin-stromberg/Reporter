// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Validates feed URLs entered by the user: only absolute HTTP/HTTPS URIs are
/// accepted as feed addresses.
/// </summary>
public static class FeedUrlValidator
{
    /// <summary>
    /// Determines whether the value is an absolute HTTP or HTTPS URI.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns><see langword="true"/> when the URL parses as an absolute http/https URI.</returns>
    public static bool IsValidFeedUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
