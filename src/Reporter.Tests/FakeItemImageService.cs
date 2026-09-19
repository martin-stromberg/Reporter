// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ServiceModel.Syndication;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IItemImageService"/> fake that delegates the
/// candidate resolution to the real <see cref="ItemImageService"/> and returns
/// a configurable download result while recording the requested URLs.
/// </summary>
public sealed class FakeItemImageService : IItemImageService
{
    // Ein statisch geteilter Resolver: ResolveImageUrl nutzt den HttpClient
    // nicht, und eine gemeinsame Instanz verhindert einen HttpClient-Leak pro
    // Testklassen-Instanz.
    private static readonly ItemImageService Resolver = new(new HttpClient());

    /// <summary>
    /// Gets or sets the image returned by <see cref="TryDownloadImageAsync"/>,
    /// or <c>null</c> to simulate a failed download.
    /// </summary>
    public ItemImage? NextResult { get; set; }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="TryDownloadImageAsync"/>, or <c>null</c> for none.
    /// </summary>
    public Exception? NextException { get; set; }

    /// <summary>
    /// Gets the image URLs passed to <see cref="TryDownloadImageAsync"/> in call order.
    /// </summary>
    public List<string> RequestedUrls { get; } = [];

    /// <summary>
    /// Gets the cancellation tokens passed to <see cref="TryDownloadImageAsync"/> in call order.
    /// </summary>
    public List<CancellationToken> ReceivedCancellationTokens { get; } = [];

    /// <inheritdoc />
    public string? ResolveImageUrl(SyndicationItem feedItem, string? contentHtml, string? itemLink, string? feedUrl)
    {
        return Resolver.ResolveImageUrl(feedItem, contentHtml, itemLink, feedUrl);
    }

    /// <inheritdoc />
    public Task<ItemImage?> TryDownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(imageUrl);
        ReceivedCancellationTokens.Add(cancellationToken);
        return NextException is not null
            ? Task.FromException<ItemImage?>(NextException)
            : Task.FromResult(NextResult);
    }
}
