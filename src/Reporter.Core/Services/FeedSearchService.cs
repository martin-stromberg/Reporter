// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text.Json;
using System.Text.RegularExpressions;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Searches for feeds using the feedsearch.dev directory and client-side feed
/// autodiscovery (HTML link tags and well-known feed paths). Both sources share a
/// per-call time budget; an exception is only raised when both sources fail.
/// </summary>
public class FeedSearchService : IFeedSearchService
{
    private const string DirectoryEndpoint = "https://feedsearch.dev/api/v1/search";
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly string[] FeedMediaTypes =
    [
        "application/rss+xml",
        "application/atom+xml",
        "application/feed+json",
        "application/xml",
        "text/xml",
    ];

    private static readonly string[] FeedLinkMediaTypes =
    [
        "application/rss+xml",
        "application/atom+xml",
        "application/feed+json",
    ];

    private static readonly string[] StandardFeedPaths =
    [
        "/feed",
        "/rss",
        "/rss.xml",
        "/atom.xml",
        "/feed.xml",
        "/index.xml",
    ];

    private static readonly Regex LinkTagPattern = new("<link\\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeout);
    private static readonly Regex AttributePattern = new("(\\w+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))", RegexOptions.IgnoreCase, RegexTimeout);

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSearchService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for the directory and website requests.</param>
    public FeedSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeedSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SearchTimeout);
        var token = timeout.Token;

        var results = new List<FeedSearchResult>();
        var directoryFailed = false;
        var discoveryFailed = false;

        try
        {
            results = await SearchDirectoryAsync(query, token).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Only failures of the source itself (including the internal time
            // budget) count here; a cancellation requested by the caller must
            // propagate instead of being masked as an unavailable source.
            directoryFailed = true;
        }

        if (results.Count == 0)
        {
            try
            {
                results = await DiscoverFeedsAsync(query, token).ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                discoveryFailed = true;
            }
        }

        if (directoryFailed && discoveryFailed)
        {
            throw new FeedSearchUnavailableException("Neither the feed directory nor autodiscovery could be reached.");
        }

        return results
            .GroupBy(result => result.FeedUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(result => result.MatchKind)
            .ThenByDescending(result => result.Score)
            .ThenBy(result => result.FeedUrl, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<List<FeedSearchResult>> SearchDirectoryAsync(string query, CancellationToken cancellationToken)
    {
        var requestUri = $"{DirectoryEndpoint}?url={Uri.EscapeDataString(query)}&info=true&favicon=false&opml=false&skip_crawl=true";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The feed directory did not return a JSON array.");
        }

        var results = new List<FeedSearchResult>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var feedUrl = GetString(entry, "url");
            if (string.IsNullOrWhiteSpace(feedUrl))
            {
                continue;
            }

            if (entry.TryGetProperty("bozo", out var bozo) &&
                ((bozo.ValueKind == JsonValueKind.Number && bozo.GetInt32() == 1) || bozo.ValueKind == JsonValueKind.True))
            {
                continue;
            }

            var score = entry.TryGetProperty("score", out var scoreElement) && scoreElement.ValueKind == JsonValueKind.Number
                ? scoreElement.GetDouble()
                : 0d;

            results.Add(new FeedSearchResult
            {
                Title = GetString(entry, "title"),
                Description = GetString(entry, "description"),
                SiteName = GetString(entry, "site_name"),
                SiteUrl = GetString(entry, "site_url"),
                FeedUrl = feedUrl,
                Score = score,
                MatchKind = string.Equals(feedUrl, query, StringComparison.OrdinalIgnoreCase)
                    ? FeedSearchMatchKind.ExactUrl
                    : FeedSearchMatchKind.Directory,
            });
        }

        return results;
    }

    private async Task<List<FeedSearchResult>> DiscoverFeedsAsync(string query, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (IsFeedMediaType(mediaType))
        {
            return
            [
                new FeedSearchResult
                {
                    FeedUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? query,
                    MatchKind = FeedSearchMatchKind.ExactUrl,
                },
            ];
        }

        var results = new List<FeedSearchResult>();
        var baseUri = response.RequestMessage?.RequestUri ?? new Uri(query);
        var siteUrl = baseUri.GetLeftPart(UriPartial.Authority);

        if (string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase))
        {
            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            foreach (var link in ExtractFeedLinks(html))
            {
                if (Uri.TryCreate(baseUri, link.Href, out var feedUri) &&
                    (feedUri.Scheme == Uri.UriSchemeHttp || feedUri.Scheme == Uri.UriSchemeHttps))
                {
                    results.Add(new FeedSearchResult
                    {
                        Title = link.Title,
                        SiteUrl = siteUrl,
                        FeedUrl = feedUri.AbsoluteUri,
                        MatchKind = FeedSearchMatchKind.Discovered,
                    });
                }
            }
        }

        if (results.Count == 0 && !cancellationToken.IsCancellationRequested)
        {
            results.AddRange(await ProbeStandardPathsAsync(baseUri, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    private async Task<List<FeedSearchResult>> ProbeStandardPathsAsync(Uri baseUri, CancellationToken cancellationToken)
    {
        var results = new List<FeedSearchResult>();
        var origin = new Uri(baseUri.GetLeftPart(UriPartial.Authority));

        foreach (var path in StandardFeedPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = new Uri(origin, path);
            try
            {
                using var response = await _httpClient.GetAsync(candidate, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && IsFeedMediaType(response.Content.Headers.ContentType?.MediaType))
                {
                    results.Add(new FeedSearchResult
                    {
                        SiteUrl = origin.AbsoluteUri,
                        FeedUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? candidate.AbsoluteUri,
                        MatchKind = FeedSearchMatchKind.Discovered,
                    });
                }
            }
            catch (HttpRequestException)
            {
                // A single unreachable candidate does not abort the remaining probes.
            }
        }

        return results;
    }

    private static IEnumerable<(string Href, string? Title)> ExtractFeedLinks(string html)
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
                !rel.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains("alternate", StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!attributes.TryGetValue("type", out var type) ||
                !FeedLinkMediaTypes.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!attributes.TryGetValue("href", out var href) || string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            attributes.TryGetValue("title", out var title);
            yield return (href, title);
        }
    }

    private static bool IsFeedMediaType(string? mediaType)
    {
        return mediaType is not null && FeedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
