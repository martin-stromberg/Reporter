// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Resolves the website URL of a feed from a known site URL, falling back to
/// the authority of the feed URL.
/// </summary>
public static class FeedSiteResolver
{
    /// <summary>
    /// Resolves the site URL to use for a favicon lookup.
    /// </summary>
    /// <param name="feedUrl">The feed URL whose authority serves as the site fallback.</param>
    /// <param name="siteUrl">The known site URL, or <c>null</c>/empty to fall back to the feed URL's authority.</param>
    /// <returns>
    /// The resolved site URL, or <c>null</c> when neither the site URL nor the
    /// feed URL yields one.
    /// </returns>
    public static string? ResolveSiteUrl(string feedUrl, string? siteUrl)
    {
        if (string.IsNullOrWhiteSpace(siteUrl) &&
            Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri))
        {
            siteUrl = feedUri.GetLeftPart(UriPartial.Authority);
        }

        return string.IsNullOrWhiteSpace(siteUrl) ? null : siteUrl;
    }
}
