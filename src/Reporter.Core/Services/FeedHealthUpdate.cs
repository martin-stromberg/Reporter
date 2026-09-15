// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Bundles the optional fields updated together with a feed's health status
/// after a synchronization run.
/// </summary>
/// <param name="ResolvedTitle">The feed title resolved from the feed document, if any.</param>
/// <param name="FaviconUrl">The favicon URL discovered during the sync, if any.</param>
/// <param name="ErrorKind">The classified error kind, or <c>null</c> to clear it.</param>
/// <param name="ErrorMessage">The raw technical error message, or <c>null</c> to clear it.</param>
/// <returns>A new <see cref="FeedHealthUpdate"/> instance.</returns>
public record FeedHealthUpdate(
    string? ResolvedTitle = null,
    string? FaviconUrl = null,
    string? ErrorKind = null,
    string? ErrorMessage = null);
