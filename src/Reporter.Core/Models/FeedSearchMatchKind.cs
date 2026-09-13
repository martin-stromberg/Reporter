// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Describes how a feed search result was found. The declaration order defines the
/// result sort order: exact URL matches first, then directory hits, then autodiscovered feeds.
/// </summary>
public enum FeedSearchMatchKind
{
    /// <summary>
    /// The result's feed URL matches the entered URL exactly, or the entered URL is itself a feed document.
    /// </summary>
    ExactUrl,

    /// <summary>
    /// The result was returned by the feedsearch.dev directory.
    /// </summary>
    Directory,

    /// <summary>
    /// The result was discovered on the entered website via link tags or well-known feed paths.
    /// </summary>
    Discovered,
}
