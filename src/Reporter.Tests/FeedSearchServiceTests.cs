// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Models;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains unit tests for the <see cref="FeedSearchService"/> class.
/// </summary>
public class FeedSearchServiceTests
{
    private static FeedSearchService CreateService(StubHttpMessageHandler handler)
    {
        return new FeedSearchService(new HttpClient(handler));
    }

    /// <summary>
    /// Verifies that feedsearch.dev JSON entries are fully mapped to <see cref="FeedSearchResult"/>
    /// and that entries flagged with <c>bozo == 1</c> are dropped.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_MapsDirectoryEntries_ToFeedSearchResults()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", """
            [
              { "title": "Example Feed", "url": "https://example.com/rss", "site_url": "https://example.com",
                "site_name": "Example Site", "description": "An example feed", "score": 0.8, "bozo": 0 },
              { "title": "Broken", "url": "https://example.com/broken", "bozo": 1 },
              { "url": "https://example.com/atom", "score": 0.5 }
            ]
            """, "application/json");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        Assert.Equal(2, results.Count);
        var first = results[0];
        Assert.Equal("Example Feed", first.Title);
        Assert.Equal("An example feed", first.Description);
        Assert.Equal("Example Site", first.SiteName);
        Assert.Equal("https://example.com", first.SiteUrl);
        Assert.Equal("https://example.com/rss", first.FeedUrl);
        Assert.Equal(0.8, first.Score);
        Assert.Equal(FeedSearchMatchKind.Directory, first.MatchKind);
        Assert.Equal("https://example.com/atom", results[1].FeedUrl);
    }

    /// <summary>
    /// Verifies that results are sorted by <see cref="FeedSearchMatchKind"/>, then descending
    /// <see cref="FeedSearchResult.Score"/> and finally ordinal <see cref="FeedSearchResult.FeedUrl"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_SortsByMatchKindThenScoreThenFeedUrl()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", """
            [
              { "url": "https://example.com/low", "score": 0.1 },
              { "url": "https://b.example.com/x", "score": 0.5 },
              { "url": "https://example.com/feed", "score": 0.4 },
              { "url": "https://example.com/high", "score": 0.9 },
              { "url": "https://a.example.com/x", "score": 0.5 }
            ]
            """, "application/json");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com/feed");

        Assert.Equal(
            ["https://example.com/feed", "https://example.com/high", "https://a.example.com/x", "https://b.example.com/x", "https://example.com/low"],
            results.Select(r => r.FeedUrl).ToList());
        Assert.Equal(FeedSearchMatchKind.ExactUrl, results[0].MatchKind);
    }

    /// <summary>
    /// Verifies that a directory entry whose URL equals the query is marked
    /// <see cref="FeedSearchMatchKind.ExactUrl"/> and ranked first.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_WhenResultFeedUrlEqualsQuery_RanksExactUrlFirst()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", """
            [
              { "url": "https://other.example.com/x", "score": 0.9 },
              { "url": "https://example.com/feed", "score": 0.1 }
            ]
            """, "application/json");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com/feed");

        Assert.Equal("https://example.com/feed", results[0].FeedUrl);
        Assert.Equal(FeedSearchMatchKind.ExactUrl, results[0].MatchKind);
        Assert.Equal(FeedSearchMatchKind.Directory, results[1].MatchKind);
    }

    /// <summary>
    /// Verifies that an empty directory response triggers autodiscovery, resolving
    /// relative <c>&lt;link rel="alternate"&gt;</c> hrefs against the site URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DirectoryEmpty_FallsBackToAutodiscovery_LinkTags()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExact("https://example.com/", """
            <html><head>
              <link rel="alternate" type="application/rss+xml" href="/feed" title="Example Feed">
            </head><body>Hi</body></html>
            """, "text/html");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Discovered, result.MatchKind);
        Assert.Equal("https://example.com/feed", result.FeedUrl);
        Assert.Equal("Example Feed", result.Title);
    }

    /// <summary>
    /// Verifies that well-known feed paths are probed when the site HTML contains no link tags.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DirectoryEmpty_ProbesStandardPaths()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExact("https://example.com/", "<html><head></head><body>Hi</body></html>", "text/html");
        handler.OnExact("https://example.com/feed", "<rss version=\"2.0\"></rss>", "application/rss+xml");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Discovered, result.MatchKind);
        Assert.Equal("https://example.com/feed", result.FeedUrl);
        Assert.Contains(handler.RequestedUrls, url => url == "https://example.com/feed");
    }

    /// <summary>
    /// Verifies that a query pointing directly at a feed document yields a single
    /// <see cref="FeedSearchMatchKind.ExactUrl"/> result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_WhenQueryIsFeedDocument_ReturnsExactUrlResult()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExact("https://example.com/feed.xml", "<rss version=\"2.0\"></rss>", "application/rss+xml");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com/feed.xml");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.ExactUrl, result.MatchKind);
        Assert.Equal("https://example.com/feed.xml", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that a query pointing at a feed document behind a redirect stores the
    /// final request URI, not the originally entered redirecting address.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_WhenQueryIsFeedDocumentAfterRedirect_ReturnsFinalUrl()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExactRedirect("https://example.com/feed", "https://www.example.com/feed", "<rss version=\"2.0\"></rss>", "application/rss+xml");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com/feed");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.ExactUrl, result.MatchKind);
        Assert.Equal("https://www.example.com/feed", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that a well-known feed path answering through a redirect stores the
    /// final request URI, not the probed candidate address.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_WhenStandardPathRespondsAfterRedirect_ReturnsFinalUrl()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExact("https://example.com/", "<html><head></head><body>Hi</body></html>", "text/html");
        handler.OnExactRedirect("https://example.com/feed", "https://example.com/rss/feed", "<rss version=\"2.0\"></rss>", "application/rss+xml");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Discovered, result.MatchKind);
        Assert.Equal("https://example.com/rss/feed", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that a directory failure still returns autodiscovery results
    /// without throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DirectoryError_ButSiteReachable_ReturnsDiscoveredResults()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", HttpStatusCode.InternalServerError);
        handler.OnExact("https://example.com/", """
            <html><head>
              <link rel="alternate" type="application/atom+xml" href="https://example.com/atom.xml">
            </head></html>
            """, "text/html");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Discovered, result.MatchKind);
        Assert.Equal("https://example.com/atom.xml", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that <see cref="FeedSearchUnavailableException"/> is thrown only when
    /// both the directory and the autodiscovery fail.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_BothSourcesFail_ThrowsFeedSearchUnavailableException()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", HttpStatusCode.InternalServerError);
        handler.Fallback = HttpStatusCode.BadGateway;
        var service = CreateService(handler);

        await Assert.ThrowsAsync<FeedSearchUnavailableException>(() => service.SearchAsync("https://example.com"));
    }

    /// <summary>
    /// Verifies that a response slower than the per-call budget is cancelled and wrapped
    /// in <see cref="FeedSearchUnavailableException"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_Timeout_ThrowsFeedSearchUnavailableException()
    {
        var handler = new StubHttpMessageHandler { Delay = TimeSpan.FromSeconds(5) };
        handler.On("feedsearch.dev", "[]", "application/json");
        var service = CreateService(handler);

        await Assert.ThrowsAsync<FeedSearchUnavailableException>(() => service.SearchAsync("https://example.com"));
    }

    /// <summary>
    /// Verifies that a cancellation requested by the caller propagates as
    /// <see cref="OperationCanceledException"/> instead of being masked as a
    /// <see cref="FeedSearchUnavailableException"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_WhenCallerCancels_ThrowsOperationCanceledException()
    {
        var handler = new StubHttpMessageHandler { Delay = TimeSpan.FromSeconds(5) };
        handler.On("feedsearch.dev", "[]", "application/json");
        var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SearchAsync("https://example.com", cancellation.Token));
    }

    /// <summary>
    /// Verifies that malformed directory JSON is treated like a directory failure
    /// so autodiscovery still runs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_MalformedDirectoryJson_FallsBackToAutodiscovery()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "this is not json", "application/json");
        handler.OnExact("https://example.com/", """
            <html><head>
              <link rel="alternate" type="application/rss+xml" href="/rss.xml">
            </head></html>
            """, "text/html");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Discovered, result.MatchKind);
        Assert.Equal("https://example.com/rss.xml", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that duplicate feed URLs within the directory response are merged
    /// into a single entry, compared case-insensitively.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DirectoryDuplicates_DedupesByFeedUrl()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", """
            [
              { "url": "https://example.com/feed", "score": 0.5 },
              { "url": "https://EXAMPLE.com/feed", "score": 0.9 }
            ]
            """, "application/json");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        var result = Assert.Single(results);
        Assert.Equal(FeedSearchMatchKind.Directory, result.MatchKind);
        Assert.Equal("https://example.com/feed", result.FeedUrl);
    }

    /// <summary>
    /// Verifies that duplicate feed URLs within the autodiscovery results are
    /// merged into a single entry, compared case-insensitively.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DiscoveryDuplicates_DedupesByFeedUrl()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", HttpStatusCode.InternalServerError);
        handler.OnExact("https://example.com/", """
            <html><head>
              <link rel="alternate" type="application/rss+xml" href="/feed">
              <link rel="alternate" type="application/rss+xml" href="https://example.com/feed">
            </head></html>
            """, "text/html");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        Assert.Single(results);
    }

    /// <summary>
    /// Verifies that a non-empty directory result skips the autodiscovery request
    /// entirely (fast path).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_DirectoryResultsPresent_SkipsAutodiscovery()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", """
            [ { "url": "https://example.com/feed", "score": 0.9 } ]
            """, "application/json");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        Assert.Single(results);
        Assert.Single(handler.RequestedUrls);
        Assert.Contains("feedsearch.dev", handler.RequestedUrls[0]);
    }

    /// <summary>
    /// Verifies that an empty directory response and a site without discoverable feeds
    /// return an empty list instead of throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SearchAsync_EverythingEmpty_ReturnsEmptyList()
    {
        var handler = new StubHttpMessageHandler();
        handler.On("feedsearch.dev", "[]", "application/json");
        handler.OnExact("https://example.com/", "<html><head></head><body>Hi</body></html>", "text/html");
        var service = CreateService(handler);

        var results = await service.SearchAsync("https://example.com");

        Assert.Empty(results);
    }

    /// <summary>
    /// A stub <see cref="HttpMessageHandler"/> that returns configured responses per
    /// request URL and records every requested URL.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(Func<string, bool> Match, Func<HttpResponseMessage> Respond)> _rules = [];
        private readonly List<string> _requestedUrls = [];

        /// <summary>
        /// Gets or sets the status code returned for requests that match no rule.
        /// </summary>
        public HttpStatusCode Fallback { get; set; } = HttpStatusCode.NotFound;

        /// <summary>
        /// Gets or sets an optional per-request delay, used to trigger the per-call timeout.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Gets the URLs of all requests made so far, in order.
        /// </summary>
        public IReadOnlyList<string> RequestedUrls => _requestedUrls;

        /// <summary>
        /// Registers a response for every request whose URL contains the given fragment.
        /// </summary>
        /// <param name="urlContains">The URL fragment to match.</param>
        /// <param name="content">The response body.</param>
        /// <param name="mediaType">The response content type.</param>
        public void On(string urlContains, string content, string mediaType)
        {
            _rules.Add((url => url.Contains(urlContains, StringComparison.OrdinalIgnoreCase),
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, mediaType),
                }));
        }

        /// <summary>
        /// Registers an error response for every request whose URL contains the given fragment.
        /// </summary>
        /// <param name="urlContains">The URL fragment to match.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        public void On(string urlContains, HttpStatusCode statusCode)
        {
            _rules.Add((url => url.Contains(urlContains, StringComparison.OrdinalIgnoreCase),
                () => new HttpResponseMessage(statusCode)));
        }

        /// <summary>
        /// Registers a response for a request whose URL equals the given URL exactly.
        /// </summary>
        /// <param name="exactUrl">The exact request URL to match.</param>
        /// <param name="content">The response body.</param>
        /// <param name="mediaType">The response content type.</param>
        public void OnExact(string exactUrl, string content, string mediaType)
        {
            _rules.Add((url => string.Equals(url, exactUrl, StringComparison.OrdinalIgnoreCase),
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, mediaType),
                }));
        }

        /// <summary>
        /// Registers a response for a request whose URL equals the given URL exactly,
        /// with the response's request message pointing at a different URL — this
        /// simulates a redirect that the HTTP stack already followed transparently.
        /// </summary>
        /// <param name="exactUrl">The exact request URL to match.</param>
        /// <param name="finalUrl">The URL the (virtual) redirect chain ended at.</param>
        /// <param name="content">The response body.</param>
        /// <param name="mediaType">The response content type.</param>
        public void OnExactRedirect(string exactUrl, string finalUrl, string content, string mediaType)
        {
            _rules.Add((url => string.Equals(url, exactUrl, StringComparison.OrdinalIgnoreCase),
                () => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, mediaType),
                    RequestMessage = new HttpRequestMessage(HttpMethod.Get, finalUrl),
                }));
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;
            _requestedUrls.Add(url);

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            foreach (var (match, respond) in _rules)
            {
                if (match(url))
                {
                    return respond();
                }
            }

            return new HttpResponseMessage(Fallback);
        }
    }
}
