// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Derives the single-letter text shown in the fallback avatar circle when a
/// feed has no displayable favicon.
/// </summary>
internal static class FeedAvatar
{
    /// <summary>
    /// Returns the first letter of <paramref name="title"/> in upper case,
    /// or <c>"?"</c> when the title is empty.
    /// </summary>
    /// <param name="title">The feed title to derive the initial from.</param>
    /// <returns>The avatar initial letter.</returns>
    internal static string Initial(string? title)
    {
        return string.IsNullOrWhiteSpace(title) ? "?" : title[..1].ToUpperInvariant();
    }
}
