// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Reporter.Core.Interfaces;

namespace Reporter.Core.Services;

/// <summary>
/// Discovers the favicon URL of a website: the site HTML is scanned for
/// <c>&lt;link rel="icon|shortcut icon|apple-touch-icon"&gt;</c> tags and each
/// candidate — including the <c>/favicon.ico</c> fallback — is verified with a
/// request before it is returned.
/// </summary>
public class FeedIconService : IFeedIconService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex LinkTagPattern = new("<link\\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeout);
    private static readonly Regex AttributePattern = new("(\\w+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))", RegexOptions.IgnoreCase, RegexTimeout);

    private static readonly string[] IconRelValues =
    [
        "icon",
        "shortcut icon",
        "apple-touch-icon",
    ];

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedIconService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for the website and icon requests.</param>
    public FeedIconService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<string?> FindFaviconUrlAsync(string siteUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var siteUri) ||
            (siteUri.Scheme != Uri.UriSchemeHttp && siteUri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var candidates = new List<Uri>();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, siteUri);
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (response.IsSuccessStatusCode && string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var baseUri = response.RequestMessage?.RequestUri ?? siteUri;
                candidates.AddRange(ExtractIconLinks(html, baseUri));
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // An unreadable site page only means no link-tag candidates; the
            // /favicon.ico fallback below still applies.
        }

        var origin = new Uri(siteUri.GetLeftPart(UriPartial.Authority));
        candidates.Add(new Uri(origin, "/favicon.ico"));

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await VerifyAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
                return candidate.AbsoluteUri;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string?> TryFindFaviconUrlAsync(string feedUrl, string? siteUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedSiteUrl = FeedSiteResolver.ResolveSiteUrl(feedUrl, siteUrl);
            return resolvedSiteUrl is null
                ? null
                : await FindFaviconUrlAsync(resolvedSiteUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Favicon lookup failed for '{siteUrl}': {ex}");
            return null;
        }
    }

    private static IEnumerable<Uri> ExtractIconLinks(string html, Uri baseUri)
    {
        var searchArea = html;
        var headStart = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        if (headStart >= 0)
        {
            var headEnd = html.IndexOf("</head>", headStart, StringComparison.OrdinalIgnoreCase);
            searchArea = headEnd > headStart ? html[headStart..headEnd] : html[headStart..];
        }

        foreach (Match tagMatch in LinkTagPattern.Matches(searchArea))
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match attributeMatch in AttributePattern.Matches(tagMatch.Value))
            {
                attributes[attributeMatch.Groups[1].Value] =
                    attributeMatch.Groups[2].Success ? attributeMatch.Groups[2].Value :
                    attributeMatch.Groups[3].Success ? attributeMatch.Groups[3].Value :
                    attributeMatch.Groups[4].Value;
            }

            if (!attributes.TryGetValue("rel", out var rel) ||
                !IconRelValues.Contains(rel.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!attributes.TryGetValue("href", out var href) || string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (Uri.TryCreate(baseUri, href, out var iconUri) &&
                (iconUri.Scheme == Uri.UriSchemeHttp || iconUri.Scheme == Uri.UriSchemeHttps))
            {
                yield return iconUri;
            }
        }
    }

    private async Task<bool> VerifyAsync(Uri iconUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(iconUri, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
