// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a single feed search hit. This is a pure in-memory model; it is not persisted.
/// </summary>
public class FeedSearchResult
{
    /// <summary>
    /// Gets the title of the feed, if known.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the description of the feed, if known. Shown in the result card only; never persisted.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the name of the website the feed belongs to, if known.
    /// </summary>
    public string? SiteName { get; init; }

    /// <summary>
    /// Gets the URL of the website the feed belongs to, if known.
    /// </summary>
    public string? SiteUrl { get; init; }

    /// <summary>
    /// Gets the URL of the feed document.
    /// </summary>
    public required string FeedUrl { get; init; }

    /// <summary>
    /// Gets the relevance score reported by the feed directory. Autodiscovery hits use <c>0</c>.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Gets the kind of match that produced this result.
    /// </summary>
    public FeedSearchMatchKind MatchKind { get; init; }
}
