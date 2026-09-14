// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IFeedIconService"/> fake that records the requested
/// site URLs and returns a configurable result.
/// </summary>
public sealed class FakeFeedIconService : IFeedIconService
{
    /// <summary>
    /// Gets or sets the favicon URL returned by <see cref="FindFaviconUrlAsync"/>,
    /// or <c>null</c> to simulate a failed lookup.
    /// </summary>
    public string? NextResult { get; set; }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="FindFaviconUrlAsync"/>, or <c>null</c> for none.
    /// </summary>
    public Exception? NextException { get; set; }

    /// <summary>
    /// Gets the site URLs passed to <see cref="FindFaviconUrlAsync"/> in call order.
    /// </summary>
    public List<string> RequestedSiteUrls { get; } = [];

    /// <summary>
    /// Gets the cancellation tokens passed to <see cref="TryFindFaviconUrlAsync"/> in call order.
    /// </summary>
    public List<CancellationToken> ReceivedCancellationTokens { get; } = [];

    /// <inheritdoc />
    public Task<string?> FindFaviconUrlAsync(string siteUrl, CancellationToken cancellationToken = default)
    {
        RequestedSiteUrls.Add(siteUrl);
        return NextException is not null
            ? Task.FromException<string?>(NextException)
            : Task.FromResult(NextResult);
    }

    /// <inheritdoc />
    public async Task<string?> TryFindFaviconUrlAsync(string feedUrl, string? siteUrl, CancellationToken cancellationToken = default)
    {
        ReceivedCancellationTokens.Add(cancellationToken);
        try
        {
            var resolvedSiteUrl = FeedSiteResolver.ResolveSiteUrl(feedUrl, siteUrl);
            return resolvedSiteUrl is null
                ? null
                : await FindFaviconUrlAsync(resolvedSiteUrl, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
