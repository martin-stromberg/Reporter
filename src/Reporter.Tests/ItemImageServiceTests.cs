// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Xml;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ItemImageService"/> class.
/// </summary>
public class ItemImageServiceTests
{
    private const int OverLimitBytes = (5 * 1024 * 1024) + 1;

    private static ItemImageService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return CreateService(request => Task.FromResult(responseFactory(request)));
    }

    private static ItemImageService CreateService(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
    {
        return new ItemImageService(new HttpClient(new FakeHttpMessageHandler(responseFactory)));
    }

    private static SyndicationItem ParseItem(string feedXml)
    {
        using var reader = XmlReader.Create(new StringReader(feedXml));
        return SyndicationFeed.Load(reader).Items.Single();
    }

    /// <summary>
    /// Verifies that an enclosure link with an <c>image/*</c> MIME type wins
    /// over the first <c>&lt;img src&gt;</c> of the content.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_EnclosureImageType_Wins()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem(TestFeedXml.RssWithEnclosure(
        [
            ("Item", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc", "https://example.com/enclosure.png", "image/png"),
        ]));

        var result = service.ResolveImageUrl(
            item,
            "<p><img src=\"https://example.com/inline.png\" /></p>",
            "https://example.com/1",
            "https://example.com/feed");

        Assert.Equal("https://example.com/enclosure.png", result);
    }

    /// <summary>
    /// Verifies that a MediaRSS <c>media:thumbnail</c> element is used when no
    /// image enclosure exists.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_MediaRss_Used()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem("""
            <?xml version="1.0"?>
            <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/">
              <channel><title>F</title>
                <item><title>T</title><link>https://example.com/1</link><guid>g</guid>
                  <media:thumbnail url="https://example.com/thumb.jpg" />
                </item>
              </channel>
            </rss>
            """);

        var result = service.ResolveImageUrl(item, null, "https://example.com/1", "https://example.com/feed");

        Assert.Equal("https://example.com/thumb.jpg", result);
    }

    /// <summary>
    /// Verifies that a MediaRSS <c>media:content</c> element with an
    /// <c>image/*</c> type is used as an image candidate.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_MediaRssContent_Used()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem("""
            <?xml version="1.0"?>
            <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/">
              <channel><title>F</title>
                <item><title>T</title><link>https://example.com/1</link><guid>g</guid>
                  <media:content url="https://example.com/media.jpg" type="image/jpeg" />
                </item>
              </channel>
            </rss>
            """);

        var result = service.ResolveImageUrl(item, null, "https://example.com/1", "https://example.com/feed");

        Assert.Equal("https://example.com/media.jpg", result);
    }

    /// <summary>
    /// Verifies that an <c>itunes:image</c> element is used when neither an
    /// image enclosure nor a MediaRSS element exists.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_ItunesImage_Used()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem("""
            <?xml version="1.0"?>
            <rss version="2.0" xmlns:itunes="http://www.itunes.com/dtds/podcast-1.0.dtd">
              <channel><title>F</title>
                <item><title>T</title><link>https://example.com/1</link><guid>g</guid>
                  <itunes:image href="https://example.com/cover.jpg" />
                </item>
              </channel>
            </rss>
            """);

        var result = service.ResolveImageUrl(item, null, "https://example.com/1", "https://example.com/feed");

        Assert.Equal("https://example.com/cover.jpg", result);
    }

    /// <summary>
    /// Verifies that the first <c>&lt;img src&gt;</c> of the content is the
    /// fallback candidate.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_FirstImgSrc_Fallback()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem(TestFeedXml.Rss(
        [
            ("Item", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc"),
        ]));

        var result = service.ResolveImageUrl(
            item,
            "<p>A <img src=\"https://example.com/first.png\" /><img src=\"https://example.com/second.png\" /></p>",
            "https://example.com/1",
            "https://example.com/feed");

        Assert.Equal("https://example.com/first.png", result);
    }

    /// <summary>
    /// Verifies that an item without any image candidate resolves to <c>null</c>.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_NoCandidate_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem(TestFeedXml.Rss(
        [
            ("Item", "https://example.com/1", "guid-1", DateTime.UtcNow, "Desc"),
        ]));

        var result = service.ResolveImageUrl(item, "<p>no image</p>", "https://example.com/1", "https://example.com/feed");

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that a relative image URL is resolved against the item link.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_RelativeUrl_ResolvedAgainstItemLink()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem(TestFeedXml.Rss(
        [
            ("Item", "https://example.com/articles/1", "guid-1", DateTime.UtcNow, "Desc"),
        ]));

        var result = service.ResolveImageUrl(
            item,
            "<p><img src=\"/images/hero.png\" /></p>",
            "https://example.com/articles/1",
            "https://other.example/feed");

        Assert.Equal("https://example.com/images/hero.png", result);
    }

    /// <summary>
    /// Verifies that a relative image URL falls back to the feed URL when no
    /// item link is available.
    /// </summary>
    [Fact]
    public void ResolveImageUrl_RelativeUrl_FallsBackToFeedUrl()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var item = ParseItem(TestFeedXml.Rss(
        [
            ("Item", "https://example.com/articles/1", "guid-1", DateTime.UtcNow, "Desc"),
        ]));

        var result = service.ResolveImageUrl(
            item,
            "<p><img src=\"/images/hero.png\" /></p>",
            null,
            "https://feeds.example/rss.xml");

        Assert.Equal("https://feeds.example/images/hero.png", result);
    }

    /// <summary>
    /// Verifies that a successful download returns the image data, its MIME
    /// type and the origin URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_ValidImage_ReturnsItemImage()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var service = CreateService(_ => TestHttpResponses.Png(bytes));

        var result = await service.TryDownloadImageAsync("https://example.com/img.png", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(bytes, result.Data);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("https://example.com/img.png", result.Url);
    }

    /// <summary>
    /// Verifies that a response with a non-<c>image/*</c> content type is discarded.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_NonImageContentType_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html/>", System.Text.Encoding.UTF8, "text/html"),
        });

        var result = await service.TryDownloadImageAsync("https://example.com/page", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that a response whose <c>Content-Length</c> exceeds the cap is
    /// discarded without reading the body.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_OversizedContentLength_ReturnsNull()
    {
        var service = CreateService(_ => TestHttpResponses.Png(new byte[OverLimitBytes]));

        var result = await service.TryDownloadImageAsync("https://example.com/big.png", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that a streamed response without a <c>Content-Length</c> header
    /// that exceeds the cap while reading is discarded.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_OversizedStream_ReturnsNull()
    {
        var service = CreateService(_ =>
        {
            var content = new StreamContent(new NonSeekableReadStream(new byte[OverLimitBytes]));
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var result = await service.TryDownloadImageAsync("https://example.com/big.png", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that an HTTP error status returns <c>null</c> instead of throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_HttpError_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await service.TryDownloadImageAsync("https://example.com/missing.png", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that an invalid URL returns <c>null</c> instead of throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_InvalidUrl_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await service.TryDownloadImageAsync("not-a-url", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that a network failure returns <c>null</c> instead of throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryDownloadImageAsync_NetworkFailure_ReturnsNull()
    {
        var service = CreateService(_ => Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));

        var result = await service.TryDownloadImageAsync("https://example.com/img.png", CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// A non-seekable stream keeps <see cref="StreamContent"/> from computing a
    /// <c>Content-Length</c> header so the streaming cap is exercised.
    /// </summary>
    private sealed class NonSeekableReadStream : MemoryStream
    {
        public NonSeekableReadStream(byte[] buffer)
            : base(buffer)
        {
        }

        /// <inheritdoc />
        public override bool CanSeek => false;
    }
}
