// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Derives a placeholder title from a feed URL (last path segment, host, or the
/// URL itself) and detects such placeholder titles during synchronization.
/// </summary>
public static class FeedTitleFallback
{
    /// <summary>
    /// Gets a display title derived from the feed URL: the last non-empty,
    /// URL-decoded path segment, then the host, then the URL itself.
    /// </summary>
    /// <param name="url">The feed URL to derive the title from.</param>
    /// <returns>The derived fallback title.</returns>
    public static string GetFallbackTitle(string url)
    {
        if (TryGetFileName(url, out var fileName))
        {
            return fileName;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return url;
    }

    /// <summary>
    /// Determines whether the title equals the file-name fallback of the feed
    /// URL and therefore counts as an auto-generated placeholder.
    /// </summary>
    /// <param name="title">The stored feed title.</param>
    /// <param name="url">The feed URL the title was derived from.</param>
    /// <returns><see langword="true"/> when the title matches the last non-empty, URL-decoded path segment of the URL.</returns>
    public static bool IsFileNamePlaceholderTitle(string title, string url)
    {
        return !string.IsNullOrWhiteSpace(title) &&
            TryGetFileName(url, out var fileName) &&
            string.Equals(title, fileName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetFileName(string url, out string fileName)
    {
        fileName = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segment = uri.Segments
            .Select(s => s.TrimEnd('/'))
            .LastOrDefault(s => s.Length > 0);
        if (segment is null)
        {
            return false;
        }

        fileName = Uri.UnescapeDataString(segment);
        return fileName.Length > 0;
    }
}
