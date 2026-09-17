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

    /// <summary>
    /// Gets the base URL of the stub server (<c>http://127.0.0.1:{port}</c>), valid
    /// after <see cref="InitializeAsync"/> has completed.
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

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

        app.MapGet("/feeds/site-feed.xml", () =>
            Results.Content(File.ReadAllText(Path.Combine(fixturesDir, "site-feed.xml")), "application/rss+xml"));

        app.MapGet("/feeds/{name}.xml", (string name) =>
            Results.Content(
                File.ReadAllText(Path.Combine(fixturesDir, "stub-feed.xml"))
                    .Replace("{name}", name, StringComparison.Ordinal),
                "application/rss+xml"));

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
