// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text;

namespace Reporter.Tests;

/// <summary>
/// Builds RSS 2.0, Atom 1.0 and Atom 0.3 feed documents for tests.
/// </summary>
public static class TestFeedXml
{
    private const string WdrAppNamespace = "http://www.wdr.de/rss/1.0/modules/app/1.0/";

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
    /// Builds an RSS 2.0 document whose items carry an <c>&lt;enclosure&gt;</c>
    /// element (e.g. an article image with an <c>image/*</c> type).
    /// </summary>
    /// <param name="items">The items to include in the channel.</param>
    /// <param name="channelTitle">The channel title.</param>
    /// <returns>The RSS 2.0 document.</returns>
    public static string RssWithEnclosure(IEnumerable<(string Title, string Link, string Guid, DateTime? PubDate, string? Description, string EnclosureUrl, string EnclosureType)> items, string channelTitle = "Test Feed")
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
            builder.AppendLine($"      <enclosure url=\"{item.EnclosureUrl}\" length=\"0\" type=\"{item.EnclosureType}\" />");
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
    /// Builds an Atom 1.0 document whose entries may carry a namespaced
    /// <c>tags</c> extension element and <c>&lt;category term="…"/&gt;</c>
    /// elements; the feed itself can carry both as well (feed-level elements
    /// are not part of an item). The feed root declares the
    /// <c>xmlns:app="http://www.wdr.de/rss/1.0/modules/app/1.0/"</c> prefix;
    /// a foreign tags namespace is declared inline on the element.
    /// </summary>
    /// <param name="entries">The entries to include in the feed.</param>
    /// <param name="feedTitle">The feed title.</param>
    /// <param name="feedTags">An optional term list rendered as feed-level <c>&lt;app:tags&gt;</c>.</param>
    /// <param name="feedCategories">Optional feed-level <c>&lt;category&gt;</c> terms.</param>
    /// <returns>The Atom 1.0 document.</returns>
    public static string AtomWithTags(
        IEnumerable<(string Title, string Link, string Id, DateTime? Updated, string? Content, string? Tags, string? TagsNamespace, string[]? Categories)> entries,
        string feedTitle = "Test Feed",
        string? feedTags = null,
        string[]? feedCategories = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine($"<feed xmlns=\"http://www.w3.org/2005/Atom\" xmlns:app=\"{WdrAppNamespace}\">");
        builder.AppendLine($"  <title>{feedTitle}</title>");
        if (feedTags is not null)
        {
            builder.AppendLine($"  <app:tags>{feedTags}</app:tags>");
        }

        if (feedCategories is not null)
        {
            foreach (var category in feedCategories)
            {
                builder.AppendLine($"  <category term=\"{category}\" />");
            }
        }

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

            AppendTagsElement(builder, "    ", entry.Tags, entry.TagsNamespace);
            if (entry.Categories is not null)
            {
                foreach (var category in entry.Categories)
                {
                    builder.AppendLine($"    <category term=\"{category}\" />");
                }
            }

            builder.AppendLine("  </entry>");
        }

        builder.AppendLine("</feed>");
        return builder.ToString();
    }

    /// <summary>
    /// Builds an RSS 2.0 document whose items may carry a namespaced
    /// <c>tags</c> extension element and <c>&lt;category&gt;</c> elements.
    /// The channel root declares the
    /// <c>xmlns:app="http://www.wdr.de/rss/1.0/modules/app/1.0/"</c> prefix;
    /// a foreign tags namespace is declared inline on the element.
    /// </summary>
    /// <param name="items">The items to include in the channel.</param>
    /// <param name="channelTitle">The channel title.</param>
    /// <returns>The RSS 2.0 document.</returns>
    public static string RssWithTags(
        IEnumerable<(string Title, string Link, string Guid, DateTime? PubDate, string? Description, string? Tags, string? TagsNamespace, string[]? Categories)> items,
        string channelTitle = "Test Feed")
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine($"<rss version=\"2.0\" xmlns:app=\"{WdrAppNamespace}\">");
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

            AppendTagsElement(builder, "      ", item.Tags, item.TagsNamespace);
            if (item.Categories is not null)
            {
                foreach (var category in item.Categories)
                {
                    builder.AppendLine($"      <category>{category}</category>");
                }
            }

            builder.AppendLine("    </item>");
        }

        builder.AppendLine("  </channel>");
        builder.AppendLine("</rss>");
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

    // Renders the namespaced tags extension element: inside the app namespace
    // the declared "app" prefix is reused, foreign namespaces are declared
    // inline so the local-name-only match can be exercised for any namespace.
    private static void AppendTagsElement(StringBuilder builder, string indent, string? tags, string? tagsNamespace)
    {
        if (tags is null)
        {
            return;
        }

        if (tagsNamespace is null || string.Equals(tagsNamespace, WdrAppNamespace, StringComparison.Ordinal))
        {
            builder.AppendLine($"{indent}<app:tags>{tags}</app:tags>");
            return;
        }

        builder.AppendLine($"{indent}<x:tags xmlns:x=\"{tagsNamespace}\">{tags}</x:tags>");
    }
}
