// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Resolves the article image candidate of a feed item and downloads it
/// strictly isolated during the feed synchronization.
/// </summary>
public class ItemImageService : IItemImageService
{
    private const int MaxImageBytes = 5 * 1024 * 1024;
    private const string MediaRssNamespace = "http://search.yahoo.com/mrss/";
    private const string ItunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex ImgSrcPattern = new("<img[^>]+src\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase, RegexTimeout);

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemImageService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for the image downloads.</param>
    public ItemImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public string? ResolveImageUrl(SyndicationItem feedItem, string? contentHtml, string? itemLink, string? feedUrl)
    {
        var candidate = feedItem.Links
            .Where(l => string.Equals(l.RelationshipType, "enclosure", StringComparison.OrdinalIgnoreCase))
            .Where(l => l.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            .Select(l => l.Uri?.ToString())
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        candidate ??= ResolveMediaRssUrl(feedItem);
        candidate ??= ResolveItunesImageUrl(feedItem);
        candidate ??= ExtractFirstImageUrl(contentHtml);

        return ResolveCandidateUrl(candidate, itemLink, feedUrl);
    }

    /// <inheritdoc />
    public async Task<ItemImage?> TryDownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (response.Content.Headers.ContentLength > MaxImageBytes)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var data = await ReadCappedAsync(stream, MaxImageBytes, cancellationToken).ConfigureAwait(false);
            return data is null ? null : new ItemImage(data, contentType, uri.AbsoluteUri);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"Image download failed for '{imageUrl}': {ex}");
            return null;
        }
    }

    /// <summary>
    /// Extracts the <c>src</c> attribute of the first <c>&lt;img&gt;</c> tag of
    /// the specified HTML. Shared with <c>ItemRepository.ExtractImageUrl</c> so
    /// that the remote fallback URL and the download candidate always derive
    /// from the same image.
    /// </summary>
    /// <param name="contentHtml">The HTML content to scan.</param>
    /// <returns>The first image URL, or <c>null</c> when none is present.</returns>
    public static string? ExtractFirstImageUrl(string? contentHtml)
    {
        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            return null;
        }

        var match = ImgSrcPattern.Match(contentHtml);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ResolveMediaRssUrl(SyndicationItem feedItem)
    {
        foreach (var extension in feedItem.ElementExtensions)
        {
            if (!string.Equals(extension.OuterNamespace, MediaRssNamespace, StringComparison.Ordinal))
            {
                continue;
            }

            using var reader = extension.GetReader();
            if (string.Equals(extension.OuterName, "thumbnail", StringComparison.Ordinal))
            {
                var url = reader.GetAttribute("url");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
            else if (string.Equals(extension.OuterName, "content", StringComparison.Ordinal) &&
                IsImageMediaContent(reader))
            {
                var url = reader.GetAttribute("url");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
        }

        return null;
    }

    private static bool IsImageMediaContent(XmlReader reader)
    {
        var type = reader.GetAttribute("type");
        if (type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return string.Equals(reader.GetAttribute("medium"), "image", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveItunesImageUrl(SyndicationItem feedItem)
    {
        foreach (var extension in feedItem.ElementExtensions)
        {
            if (string.Equals(extension.OuterNamespace, ItunesNamespace, StringComparison.Ordinal) &&
                string.Equals(extension.OuterName, "image", StringComparison.Ordinal))
            {
                using var reader = extension.GetReader();
                var href = reader.GetAttribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    return href;
                }
            }
        }

        return null;
    }

    private static string? ResolveCandidateUrl(string? candidate, string? itemLink, string? feedUrl)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.AbsoluteUri;
        }

        return TryResolveAgainst(candidate, itemLink, out var resolved) ||
            TryResolveAgainst(candidate, feedUrl, out resolved)
                ? resolved
                : null;
    }

    private static bool TryResolveAgainst(string candidate, string? baseUrl, out string? resolved)
    {
        resolved = null;
        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUri, candidate, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        resolved = uri.AbsoluteUri;
        return true;
    }

    private static async Task<byte[]?> ReadCappedAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        var total = 0;
        int read;
        while ((read = await stream
            .ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, maxBytes - total + 1)), cancellationToken)
            .ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
