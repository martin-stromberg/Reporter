// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Discovers the favicon URL of a feed's website.
/// </summary>
public interface IFeedIconService
{
    /// <summary>
    /// Finds a working favicon URL for the given website by evaluating the
    /// <c>&lt;link rel="icon…"&gt;</c> tags of the site HTML and falling back to
    /// <c>/favicon.ico</c> at the site origin.
    /// </summary>
    /// <param name="siteUrl">The absolute URL of the feed's website.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// verified absolute favicon URL, or <c>null</c> when no reachable favicon was found.
    /// </returns>
    Task<string?> FindFaviconUrlAsync(string siteUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the feed's website — <paramref name="siteUrl"/>, or the authority
    /// of <paramref name="feedUrl"/> as a fallback — and looks up its favicon via
    /// <see cref="FindFaviconUrlAsync"/>. The lookup is strictly isolated: any
    /// failure returns <c>null</c> instead of throwing, so callers never need
    /// their own guard around it.
    /// </summary>
    /// <param name="feedUrl">The feed URL whose authority serves as the site fallback.</param>
    /// <param name="siteUrl">The known site URL, or <c>null</c>/empty to fall back to the feed URL's authority.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// verified absolute favicon URL, or <c>null</c> when no site URL could be
    /// resolved, no reachable favicon was found or the lookup failed.
    /// </returns>
    Task<string?> TryFindFaviconUrlAsync(string feedUrl, string? siteUrl, CancellationToken cancellationToken = default);
}
