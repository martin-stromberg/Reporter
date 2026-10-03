// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Provides feed sync warning category constants.
/// </summary>
public static class FeedSyncWarningKind
{
    /// <summary>
    /// Category for a feed that returned no items at all — the source may be
    /// empty or the address incorrect.
    /// </summary>
    public const string NoItems = "NoItems";

    /// <summary>
    /// Category for a feed that delivered no new items while its most recent
    /// stored item is older than 30 days — the source may be abandoned.
    /// </summary>
    public const string NoRecentItems = "NoRecentItems";
}
