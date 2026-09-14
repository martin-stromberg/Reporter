// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Text;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="FeedIconService"/> class.
/// </summary>
public class FeedIconServiceTests
{
    private static FeedIconService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new FeedIconService(new HttpClient(new FakeHttpMessageHandler(responseFactory)));
    }

    private static HttpResponseMessage Html(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/html"),
        };
    }

    /// <summary>
    /// Verifies that an absolute icon URL from a link tag is returned when the
    /// candidate verifies successfully.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_ParsesLinkTag()
    {
        var service = CreateService(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/")
            {
                return Html("<html><head><link rel=\"icon\" href=\"https://cdn.example.com/icon.png\"></head><body/></html>");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await service.FindFaviconUrlAsync("https://example.com/");

        Assert.Equal("https://cdn.example.com/icon.png", result);
    }

    /// <summary>
    /// Verifies that a relative icon href is resolved against the site URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_ResolvesRelativeUrl()
    {
        var service = CreateService(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/blog")
            {
                return Html("<html><head><link rel=\"icon\" href=\"/assets/favicon-32.png\"></head></html>");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await service.FindFaviconUrlAsync("https://example.com/blog");

        Assert.Equal("https://example.com/assets/favicon-32.png", result);
    }

    /// <summary>
    /// Verifies that /favicon.ico at the site origin is used when the page
    /// declares no icon link tag.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_FallsBackToFaviconIco()
    {
        var service = CreateService(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/")
            {
                return Html("<html><head><title>No icon</title></head></html>");
            }

            return request.RequestUri?.AbsoluteUri == "https://example.com/favicon.ico"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var result = await service.FindFaviconUrlAsync("https://example.com/");

        Assert.Equal("https://example.com/favicon.ico", result);
    }

    /// <summary>
    /// Verifies that an unreachable or invalid candidate is skipped and the next
    /// candidate is verified.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_BrokenLinkTagCandidate_TriesNext()
    {
        var service = CreateService(request =>
        {
            return request.RequestUri?.AbsoluteUri switch
            {
                "https://example.com/" => Html("<html><head><link rel=\"icon\" href=\"/missing.png\"></head></html>"),
                "https://example.com/favicon.ico" => new HttpResponseMessage(HttpStatusCode.OK),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        });

        var result = await service.FindFaviconUrlAsync("https://example.com/");

        Assert.Equal("https://example.com/favicon.ico", result);
    }

    /// <summary>
    /// Verifies that null is returned when no candidate verifies.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_ReturnsNullOnFailure()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await service.FindFaviconUrlAsync("https://example.com/");

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that a failing site request still attempts the /favicon.ico
    /// fallback at the origin.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_SiteRequestFails_StillTriesFallback()
    {
        var service = CreateService(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/favicon.ico")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            throw new HttpRequestException("site unreachable");
        });

        var result = await service.FindFaviconUrlAsync("https://example.com/");

        Assert.Equal("https://example.com/favicon.ico", result);
    }

    /// <summary>
    /// Verifies that a non-http(s) or malformed site URL returns null without
    /// issuing any request.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task FindFaviconUrlAsync_InvalidSiteUrl_ReturnsNull()
    {
        var requested = false;
        var service = CreateService(_ =>
        {
            requested = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        Assert.Null(await service.FindFaviconUrlAsync("not a url"));
        Assert.Null(await service.FindFaviconUrlAsync("ftp://example.com/"));
        Assert.False(requested);
    }

    /// <summary>
    /// Verifies that a given site URL is used for the favicon lookup instead of
    /// the feed URL's authority.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryFindFaviconUrlAsync_WithSiteUrl_LooksUpSiteUrl()
    {
        var requested = new List<string?>();
        var service = CreateService(request =>
        {
            requested.Add(request.RequestUri?.AbsoluteUri);
            return request.RequestUri?.AbsoluteUri == "https://site.example/favicon.ico"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var result = await service.TryFindFaviconUrlAsync("https://feeds.example.com/rss", "https://site.example");

        Assert.Equal("https://site.example/favicon.ico", result);
        Assert.DoesNotContain(requested, url => url != null && url.StartsWith("https://feeds.example.com", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a missing site URL falls back to the feed URL's authority.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryFindFaviconUrlAsync_WithoutSiteUrl_FallsBackToFeedAuthority()
    {
        var requested = new List<string?>();
        var service = CreateService(request =>
        {
            requested.Add(request.RequestUri?.AbsoluteUri);
            return request.RequestUri?.AbsoluteUri == "https://example.com/favicon.ico"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var result = await service.TryFindFaviconUrlAsync("https://example.com/rss", null);

        Assert.Equal("https://example.com/favicon.ico", result);
        Assert.Contains("https://example.com/", requested);
    }

    /// <summary>
    /// Verifies that null is returned without issuing any request when neither
    /// the site URL nor the feed URL resolves to an absolute URI.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryFindFaviconUrlAsync_UnresolvableUrls_ReturnsNull()
    {
        var requested = false;
        var service = CreateService(_ =>
        {
            requested = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        Assert.Null(await service.TryFindFaviconUrlAsync("not a url", null));
        Assert.False(requested);
    }

    /// <summary>
    /// Verifies that the isolated lookup swallows a cancellation failure and
    /// reports it as <c>null</c> — callers must never be broken by the lookup.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryFindFaviconUrlAsync_WhenCancelled_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await service.TryFindFaviconUrlAsync(
            "https://example.com/rss",
            null,
            new CancellationToken(canceled: true));

        Assert.Null(result);
    }
}
