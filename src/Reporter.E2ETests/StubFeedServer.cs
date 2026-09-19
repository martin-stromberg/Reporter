// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reporter.E2ETests;

/// <summary>
/// In-process Kestrel stub server used by the E2E suite: it serves feed documents
/// (<c>/feeds/{name}.xml</c> with a per-name channel title), a feedsearch.dev-style
/// directory endpoint (<c>/directory</c>), an autodiscovery site (<c>/site</c>) and an
/// empty site (<c>/empty</c>). Everything else answers 404 so well-known feed probes
/// and favicon lookups run into a defined dead end.
/// </summary>
public sealed class StubFeedServer : IAsyncLifetime
{
    private WebApplication? _app;
    private int _externalLinkHitCount;

    // A valid 64x48 PNG — large enough to render as a visibly sized image in
    // the article detail WebView (the E2E image assertion requires >= 40 px
    // height) while staying compact in source and well below the 5 MB cap.
    private const string StubImageBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAEAAAAAwCAYAAAChS3wfAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAC5SURBVGhD7dChbQQBFEPBrS6FpZfrIP1t+JDD1n9giIEl+/n5/Xsvewyu6QCDazrA4JoOMLjm6wHv55nmHnWAgSxc4x51gIEsXOMedYCBLFzjHnWAgSxc4x51gIEsXOMedYCBLFzjHnWAgSxc4x51gIEsXOMedYCBLFzjHnWAwTUdYHBNBxhc0wEG15w/4B9CubXDbrBBzQAAAABJRU5ErkJggg==";

    /// <summary>
    /// Gets the base URL of the stub server (<c>http://127.0.0.1:{port}</c>), valid
    /// after <see cref="InitializeAsync"/> has completed.
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the number of GET requests received on <c>/external-link</c> — the
    /// target of the external article link whose hits prove that the system
    /// browser (not the in-app WebView) fetched the URL.
    /// </summary>
    public int ExternalLinkHitCount => _externalLinkHitCount;

    /// <summary>
    /// Gets the directory endpoint URL the app under test is pointed at via
    /// <c>REPORTER_FEEDSEARCH_ENDPOINT</c>.
    /// </summary>
    public string DirectoryUrl => $"{BaseUrl}/directory";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        var fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        // The directory table is keyed by the queried site URL: only the stub
        // origin itself yields a hit so that searches for /site or /empty receive
        // an empty array and fall through to autodiscovery (SearchAsync calls
        // SearchDirectoryAsync first and only discovers when it returns no rows).
        app.MapGet("/directory", (HttpContext context) =>
        {
            var query = context.Request.Query["url"].ToString();
            var content = string.Equals(query, BaseUrl, StringComparison.OrdinalIgnoreCase)
                ? $$"""
                    [
                      {
                        "url": "{{BaseUrl}}/feeds/search-hit.xml",
                        "title": "Stub Search Hit",
                        "description": "Feed found through the stub directory",
                        "site_name": "Stub Site",
                        "site_url": "{{BaseUrl}}",
                        "score": 0.9,
                        "bozo": 0
                      }
                    ]
                    """
                : "[]";
            return Results.Content(content, "application/json");
        });

        // A dedicated fixture file (e.g. link-feed.xml, site-feed.xml) wins
        // over the generic stub-feed.xml template so tests can shape the feed
        // content; the {name} and {baseUrl} placeholders are filled either way.
        app.MapGet("/feeds/{name}.xml", (string name) =>
        {
            var namedFixture = Path.Combine(fixturesDir, $"{name}.xml");
            var source = File.Exists(namedFixture)
                ? namedFixture
                : Path.Combine(fixturesDir, "stub-feed.xml");
            return Results.Content(
                File.ReadAllText(source)
                    .Replace("{name}", name, StringComparison.Ordinal)
                    .Replace("{baseUrl}", BaseUrl, StringComparison.Ordinal),
                "application/rss+xml");
        });

        // The article image the image-feed.xml fixture references: the 64x48
        // PNG from StubImageBase64 — sized >= 40 px so the E2E image assertion
        // in ArticleImageTests can verify a visibly rendered image.
        app.MapGet("/images/{name}.png", (string name) =>
            Results.Bytes(Convert.FromBase64String(StubImageBase64), "image/png"));

        // A URL that answers with a non-image body so the app's Content-Type
        // check must reject it (broken-image-feed.xml fixture).
        app.MapGet("/not-an-image", () => Results.Content("not an image", "text/plain"));

        // Target of the external article link: hits are counted so the test can
        // prove the system browser fetched this URL instead of the WebView.
        app.MapGet("/external-link", () =>
        {
            Interlocked.Increment(ref _externalLinkHitCount);
            return Results.Content(
                "<!DOCTYPE html><html><body><p>External link target</p></body></html>",
                "text/html");
        });

        app.MapGet("/site", () =>
            Results.Content(File.ReadAllText(Path.Combine(fixturesDir, "site.html")), "text/html"));

        app.MapGet("/empty", () =>
            Results.Content(File.ReadAllText(Path.Combine(fixturesDir, "empty.html")), "text/html"));

        app.MapFallback(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });

        await app.StartAsync().ConfigureAwait(false);

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses;
        BaseUrl = addresses.Single(a => a.StartsWith("http://127.0.0.1:", StringComparison.Ordinal));
        _app = app;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync().ConfigureAwait(false);
            await _app.DisposeAsync().ConfigureAwait(false);
            _app = null;
        }
    }
}
