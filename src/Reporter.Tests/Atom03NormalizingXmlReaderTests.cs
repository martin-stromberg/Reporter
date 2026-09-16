// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.ServiceModel.Syndication;
using System.Xml;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains unit tests for the <see cref="Atom03NormalizingXmlReader"/> class.
/// </summary>
public class Atom03NormalizingXmlReaderTests
{
    private static Atom03NormalizingXmlReader Wrap(string xml)
    {
        var inner = XmlReader.Create(new StringReader(xml));
        inner.MoveToContent();
        return new Atom03NormalizingXmlReader(inner);
    }

    /// <summary>
    /// Verifies that elements in the Atom 0.3 namespace report the Atom 1.0
    /// namespace after wrapping.
    /// </summary>
    [Fact]
    public void Wrap_Atom03Root_TranslatesNamespace()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\">" +
            "<title>t</title><entry><title>e</title></entry></feed>");

        Assert.Equal("feed", reader.LocalName);
        Assert.Equal("http://www.w3.org/2005/Atom", reader.NamespaceURI);

        var elementCount = 0;
        while (reader.Read())
        {
            if (reader.NodeType is XmlNodeType.Element or XmlNodeType.EndElement)
            {
                Assert.Equal("http://www.w3.org/2005/Atom", reader.NamespaceURI);
                elementCount++;
            }
        }

        Assert.True(elementCount > 0);
    }

    /// <summary>
    /// Verifies that renamed Atom 0.3 elements are translated to their Atom 1.0
    /// names on element and end-element nodes.
    /// </summary>
    [Fact]
    public void Wrap_Atom03_RenamesElements()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\">" +
            "<tagline>t</tagline><copyright>c</copyright>" +
            "<entry><issued>2024-01-01T00:00:00Z</issued><modified>2024-01-02T00:00:00Z</modified></entry>" +
            "</feed>");

        var names = new List<string>();
        while (reader.Read())
        {
            if (reader.NodeType is XmlNodeType.Element or XmlNodeType.EndElement)
            {
                names.Add(reader.LocalName);
            }
        }

        Assert.Equal(
            ["subtitle", "subtitle", "rights", "rights", "entry", "published", "published", "updated", "updated", "entry", "feed"],
            names);
    }

    /// <summary>
    /// Verifies that MIME <c>type</c> attribute values on Atom 0.3 text and
    /// content constructs are translated to the Atom 1.0 keywords while the
    /// <c>mode</c> attribute stays untouched.
    /// </summary>
    [Fact]
    public void Wrap_ContentType_TranslatesMimeToAtom10()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\"><entry>" +
            "<summary type=\"text/html\">s</summary>" +
            "<content type=\"text/html\" mode=\"escaped\">c</content>" +
            "<title type=\"text/plain\">t</title>" +
            "</entry></feed>");

        var typeValues = new List<string?>();
        var modeValues = new List<string?>();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element &&
                reader.LocalName is "summary" or "content" or "title")
            {
                typeValues.Add(reader.GetAttribute("type"));
                modeValues.Add(reader.GetAttribute("mode"));
            }
        }

        Assert.Equal(new List<string?> { "html", "html", "text" }, typeValues);
        Assert.Equal(new List<string?> { null, "escaped", null }, modeValues);
    }

    /// <summary>
    /// Verifies that the translated <c>type</c> value is also delivered when the
    /// reader is positioned on the attribute itself.
    /// </summary>
    [Fact]
    public void Wrap_ContentType_TranslatesOnAttributePosition()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\"><entry>" +
            "<content type=\"text/html\" mode=\"escaped\">c</content>" +
            "</entry></feed>");

        while (reader.Read() && !(reader.NodeType == XmlNodeType.Element && reader.LocalName == "content"))
        {
        }

        Assert.Equal("content", reader.LocalName);
        Assert.True(reader.MoveToAttribute("type"));
        Assert.Equal("html", reader.Value);
    }

    /// <summary>
    /// Verifies that the translated <c>type</c> value is also delivered via the
    /// index-based <see cref="XmlReader.GetAttribute(int)"/> overload while
    /// other attributes keep their values and out-of-range indexes surface the
    /// inner reader's <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void Wrap_ContentType_TranslatesByIndex()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\"><entry>" +
            "<content mode=\"escaped\" type=\"text/html\">c</content>" +
            "</entry></feed>");

        while (reader.Read() && !(reader.NodeType == XmlNodeType.Element && reader.LocalName == "content"))
        {
        }

        Assert.Equal(2, reader.AttributeCount);
        Assert.Equal("escaped", reader.GetAttribute(0));
        Assert.Equal("html", reader.GetAttribute(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetAttribute(2));
    }

    /// <summary>
    /// Verifies that a <c>type</c> attribute on a text/content construct with
    /// <c>mode="base64"</c> is not translated — the value is a real MIME type
    /// describing the Base64 payload, not an Atom 0.3 format keyword.
    /// </summary>
    [Fact]
    public void Wrap_Base64Mode_TypeNotTranslated()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\"><entry>" +
            "<content type=\"image/png\" mode=\"base64\">iVBORw0KGgo=</content>" +
            "</entry></feed>");

        while (reader.Read() && !(reader.NodeType == XmlNodeType.Element && reader.LocalName == "content"))
        {
        }

        Assert.Equal("image/png", reader.GetAttribute("type"));
        Assert.True(reader.MoveToAttribute("type"));
        Assert.Equal("image/png", reader.Value);
    }

    /// <summary>
    /// Verifies that the <c>type</c> attribute of a <c>link</c> element — a MIME
    /// type in both Atom versions — is not translated.
    /// </summary>
    [Fact]
    public void Wrap_LinkType_NotTranslated()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\">" +
            "<link rel=\"alternate\" type=\"text/html\" href=\"https://example.com/\" />" +
            "<entry><link type=\"text/html\" href=\"https://example.com/1\" /></entry>" +
            "</feed>");

        var linkCount = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "link")
            {
                Assert.Equal("text/html", reader.GetAttribute("type"));
                linkCount++;
            }
        }

        Assert.Equal(2, linkCount);
    }

    /// <summary>
    /// Verifies that elements and attributes from foreign namespaces pass
    /// through unchanged.
    /// </summary>
    [Fact]
    public void Wrap_OtherNamespaces_PassThrough()
    {
        using var reader = Wrap(
            "<feed xmlns=\"http://purl.org/atom/ns#\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
            "<dc:date>2024-01-01</dc:date>" +
            "<entry dc:custom=\"x\"><title>t</title></entry>" +
            "</feed>");

        var sawDcDate = false;
        var sawDcAttribute = false;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "date")
            {
                Assert.Equal("dc", reader.Prefix);
                Assert.Equal("http://purl.org/dc/elements/1.1/", reader.NamespaceURI);
                sawDcDate = true;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "entry")
            {
                Assert.Equal("x", reader.GetAttribute("custom", "http://purl.org/dc/elements/1.1/"));
                sawDcAttribute = true;
            }
        }

        Assert.True(sawDcDate);
        Assert.True(sawDcAttribute);
    }

    /// <summary>
    /// Verifies that <see cref="SyndicationFeed.Load"/> parses a wrapped
    /// Atom 0.3 document into items with titles, links, dates and text content.
    /// </summary>
    [Fact]
    public void Load_WrappedAtom03_ProducesFeedWithItemsAndDates()
    {
        var issued = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var modified = new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc);
        var xml = TestFeedXml.Atom03(
        [
            ("Item One", "https://example.com/1", "tag:example.com,2024:1", issued, modified, "&lt;p&gt;One&lt;/p&gt;"),
        ], feedTitle: "Atom 0.3 Feed");
        var inner = XmlReader.Create(new StringReader(xml));
        inner.MoveToContent();
        using var reader = new Atom03NormalizingXmlReader(inner);

        var feed = SyndicationFeed.Load(reader);

        Assert.Equal("Atom 0.3 Feed", feed.Title?.Text);
        Assert.Contains(feed.Links, l => l.RelationshipType == "alternate" && l.Uri.ToString() == "https://example.com/");

        var item = Assert.Single(feed.Items);
        Assert.Equal("Item One", item.Title?.Text);
        Assert.Equal("tag:example.com,2024:1", item.Id);
        Assert.Equal(new DateTimeOffset(issued), item.PublishDate);
        Assert.Equal(new DateTimeOffset(modified), item.LastUpdatedTime);
        Assert.Equal("https://example.com/1", item.Links.FirstOrDefault()?.Uri?.ToString());
        var content = Assert.IsType<TextSyndicationContent>(item.Content);
        Assert.Equal("<p>One</p>", content.Text);
    }
}
