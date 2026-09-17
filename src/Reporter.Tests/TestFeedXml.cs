// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text;

namespace Reporter.Tests;

/// <summary>
/// Builds RSS 2.0, Atom 1.0 and Atom 0.3 feed documents for tests.
/// </summary>
public static class TestFeedXml
{
    /// <summary>
    /// Builds an RSS 2.0 document with the specified channel title and items.
    /// </summary>
    /// <param name="items">The items to include in the channel.</param>
    /// <param name="channelTitle">The channel title.</param>
    /// <returns>The RSS 2.0 document.</returns>
    public static string Rss(IEnumerable<(string Title, string Link, string Guid, DateTime? PubDate, string? Description)> items, string channelTitle = "Test Feed")
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<rss version=\"2.0\">");
        builder.AppendLine("  <channel>");
        builder.AppendLine($"    <title>{channelTitle}</title>");
        foreach (var item in items)
        {
            builder.AppendLine("    <item>");
            builder.AppendLine($"      <title>{item.Title}</title>");
            builder.AppendLine($"      <link>{item.Link}</link>");
            builder.AppendLine($"      <guid>{item.Guid}</guid>");
            if (item.PubDate.HasValue)
            {
                builder.AppendLine($"      <pubDate>{item.PubDate.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}</pubDate>");
            }
            if (item.Description is not null)
            {
                builder.AppendLine($"      <description>{item.Description}</description>");
            }
            builder.AppendLine("    </item>");
        }
        builder.AppendLine("  </channel>");
        builder.AppendLine("</rss>");
        return builder.ToString();
    }

    /// <summary>
    /// Builds an Atom 1.0 document with the specified feed title and entries.
    /// </summary>
    /// <param name="entries">The entries to include in the feed.</param>
    /// <param name="feedTitle">The feed title.</param>
    /// <returns>The Atom 1.0 document.</returns>
    public static string Atom(IEnumerable<(string Title, string Link, string Id, DateTime? Updated, string? Content)> entries, string feedTitle = "Test Feed")
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<feed xmlns=\"http://www.w3.org/2005/Atom\">");
        builder.AppendLine($"  <title>{feedTitle}</title>");
        foreach (var entry in entries)
        {
            builder.AppendLine("  <entry>");
            builder.AppendLine($"    <title>{entry.Title}</title>");
            builder.AppendLine($"    <link href=\"{entry.Link}\" />");
            builder.AppendLine($"    <id>{entry.Id}</id>");
            if (entry.Updated.HasValue)
            {
                builder.AppendLine($"    <updated>{entry.Updated.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}</updated>");
            }
            if (entry.Content is not null)
            {
                builder.AppendLine($"    <content type=\"html\">{entry.Content}</content>");
            }
            builder.AppendLine("  </entry>");
        }
        builder.AppendLine("</feed>");
        return builder.ToString();
    }

    /// <summary>
    /// Builds an Atom 0.3 document (namespace <c>http://purl.org/atom/ns#</c>)
    /// with the specified feed title and entries.
    /// </summary>
    /// <param name="entries">The entries to include in the feed.</param>
    /// <param name="feedTitle">The feed title.</param>
    /// <returns>The Atom 0.3 document.</returns>
    public static string Atom03(IEnumerable<(string Title, string Link, string Id, DateTime? Issued, DateTime? Modified, string? Content)> entries, string feedTitle = "Test Feed")
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<feed version=\"0.3\" xmlns=\"http://purl.org/atom/ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
        builder.AppendLine($"  <title>{feedTitle}</title>");
        builder.AppendLine("  <link rel=\"alternate\" href=\"https://example.com/\" />");
        foreach (var entry in entries)
        {
            builder.AppendLine("  <entry>");
            builder.AppendLine($"    <title>{entry.Title}</title>");
            builder.AppendLine($"    <link href=\"{entry.Link}\" />");
            builder.AppendLine($"    <id>{entry.Id}</id>");
            if (entry.Issued.HasValue)
            {
                builder.AppendLine($"    <issued>{entry.Issued.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}</issued>");
            }
            if (entry.Modified.HasValue)
            {
                builder.AppendLine($"    <modified>{entry.Modified.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}</modified>");
            }
            if (entry.Content is not null)
            {
                builder.AppendLine($"    <content type=\"text/html\" mode=\"escaped\">{entry.Content}</content>");
            }
            builder.AppendLine("  </entry>");
        }
        builder.AppendLine("</feed>");
        return builder.ToString();
    }
}
