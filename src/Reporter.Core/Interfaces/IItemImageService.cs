// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ServiceModel.Syndication;
using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Resolves the article image URL of a feed item and downloads the image
/// strictly isolated so that a failure never affects the synchronization.
/// </summary>
public interface IItemImageService
{
    /// <summary>
    /// Resolves the image candidate URL of the specified feed item using the
    /// priority enclosure/<c>link rel="enclosure"</c> with an <c>image/*</c>
    /// MIME type → MediaRSS (<c>media:thumbnail</c>/<c>media:content</c> with
    /// <c>image/*</c>) → <c>itunes:image</c> → first <c>&lt;img src&gt;</c> of
    /// <paramref name="contentHtml"/>. Relative URLs are resolved against the
    /// item link first, then against the feed URL.
    /// </summary>
    /// <param name="feedItem">The syndication item to inspect.</param>
    /// <param name="contentHtml">The item's HTML content used for the <c>&lt;img src&gt;</c> fallback.</param>
    /// <param name="itemLink">The item's link used as the primary base for relative URLs.</param>
    /// <param name="feedUrl">The feed URL used as the fallback base for relative URLs.</param>
    /// <returns>The absolute image URL, or <c>null</c> when no candidate could be resolved.</returns>
    string? ResolveImageUrl(SyndicationItem feedItem, string? contentHtml, string? itemLink, string? feedUrl);

    /// <summary>
    /// Downloads the image at the specified URL asynchronously. The download is
    /// strictly isolated: HTTP error statuses, non-<c>image/*</c> responses,
    /// oversized responses (see the internal 5 MB cap), invalid URLs and
    /// network failures all return <c>null</c> instead of throwing;
    /// <see cref="OperationCanceledException"/> propagates so a cancelled sync
    /// is not masked as a download failure.
    /// </summary>
    /// <param name="imageUrl">The absolute image URL.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded image, or <c>null</c> on any download failure.</returns>
    Task<ItemImage?> TryDownloadImageAsync(string imageUrl, CancellationToken cancellationToken);
}
