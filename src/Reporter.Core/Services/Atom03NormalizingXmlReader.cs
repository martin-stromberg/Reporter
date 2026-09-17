// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Xml;

namespace Reporter.Core.Services;

/// <summary>
/// Delegating <see cref="XmlReader"/> that presents an Atom 0.3 document
/// (namespace <see cref="NamespaceUri"/>) as an Atom 1.0 document on the fly:
/// the namespace is reported as <c>http://www.w3.org/2005/Atom</c>, renamed
/// elements are translated (<c>tagline</c>→<c>subtitle</c>,
/// <c>issued</c>→<c>published</c>, <c>modified</c>→<c>updated</c>,
/// <c>copyright</c>→<c>rights</c>) and <c>type</c> attribute values on the
/// Atom 0.3 text/content constructs are mapped from MIME types to the
/// Atom 1.0 keywords (<c>text/plain</c>→<c>text</c>,
/// <c>text/html</c>→<c>html</c>, <c>application/xhtml+xml</c>→<c>xhtml</c>).
/// All other nodes are passed through unchanged.
/// </summary>
public class Atom03NormalizingXmlReader : XmlReader
{
    /// <summary>
    /// The Atom 0.3 namespace URI that identifies a document for normalization.
    /// </summary>
    public const string NamespaceUri = "http://purl.org/atom/ns#";

    private const string Atom10NamespaceUri = "http://www.w3.org/2005/Atom";

    private readonly XmlReader _inner;
    private bool _currentElementIsTextConstruct;
    private bool _currentElementHasBase64Mode;
    private List<int>? _typeAttributeIndexes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Atom03NormalizingXmlReader"/> class.
    /// </summary>
    /// <param name="inner">The reader positioned on or before the Atom 0.3 document.</param>
    public Atom03NormalizingXmlReader(XmlReader inner)
    {
        _inner = inner;
        if (_inner.NodeType == XmlNodeType.Element)
        {
            CaptureElementContext();
        }
    }

    /// <inheritdoc />
    public override int AttributeCount => _inner.AttributeCount;

    /// <inheritdoc />
    public override string BaseURI => _inner.BaseURI;

    /// <inheritdoc />
    public override bool CanReadBinaryContent => _inner.CanReadBinaryContent;

    /// <inheritdoc />
    public override bool CanReadValueChunk => _inner.CanReadValueChunk;

    /// <inheritdoc />
    public override int Depth => _inner.Depth;

    /// <inheritdoc />
    public override bool EOF => _inner.EOF;

    /// <inheritdoc />
    public override bool HasValue => _inner.HasValue;

    /// <inheritdoc />
    public override bool IsEmptyElement => _inner.IsEmptyElement;

    /// <inheritdoc />
    public override string LocalName
    {
        get
        {
            var localName = _inner.LocalName;
            return _inner.NodeType is XmlNodeType.Element or XmlNodeType.EndElement &&
                _inner.NamespaceURI == NamespaceUri
                ? TranslateElementName(localName)
                : localName;
        }
    }

    /// <inheritdoc />
    public override string Name
    {
        get
        {
            var prefix = Prefix;
            return prefix.Length == 0 ? LocalName : string.Concat(prefix, ":", LocalName);
        }
    }

    /// <inheritdoc />
    public override string NamespaceURI =>
        _inner.NamespaceURI == NamespaceUri ? Atom10NamespaceUri : _inner.NamespaceURI;

    /// <inheritdoc />
    public override XmlNameTable NameTable => _inner.NameTable;

    /// <inheritdoc />
    public override XmlNodeType NodeType => _inner.NodeType;

    /// <inheritdoc />
    public override string Prefix => _inner.Prefix;

    /// <inheritdoc />
    public override ReadState ReadState => _inner.ReadState;

    /// <inheritdoc />
    public override XmlReaderSettings? Settings => _inner.Settings;

    /// <inheritdoc />
    public override string Value
    {
        get
        {
            var value = _inner.Value;
            if (value is not null &&
                _inner.NodeType == XmlNodeType.Attribute &&
                IsUnqualifiedTypeAttribute(_inner.LocalName, _inner.NamespaceURI) &&
                CanTranslateTypeValue)
            {
                return TranslateTypeValue(value);
            }

            return value!;
        }
    }

    /// <inheritdoc />
    public override string XmlLang => _inner.XmlLang;

    /// <inheritdoc />
    public override XmlSpace XmlSpace => _inner.XmlSpace;

    /// <inheritdoc />
    public override void Close() => _inner.Close();

    /// <inheritdoc />
    public override string GetAttribute(int i)
    {
        var value = _inner.GetAttribute(i);
        return value is not null && _typeAttributeIndexes is not null && _typeAttributeIndexes.Contains(i)
            ? TranslateTypeValue(value)
            : value!;
    }

    /// <inheritdoc />
    public override string? GetAttribute(string name)
    {
        var value = _inner.GetAttribute(name);
        return value is not null &&
            IsUnqualifiedTypeAttribute(name, null) &&
            CanTranslateTypeValue
            ? TranslateTypeValue(value)
            : value;
    }

    /// <inheritdoc />
    public override string? GetAttribute(string localName, string? namespaceURI)
    {
        var value = _inner.GetAttribute(localName, namespaceURI);
        return value is not null &&
            IsUnqualifiedTypeAttribute(localName, namespaceURI) &&
            CanTranslateTypeValue
            ? TranslateTypeValue(value)
            : value;
    }

    /// <inheritdoc />
    public override string? LookupNamespace(string prefix) => _inner.LookupNamespace(prefix);

    /// <inheritdoc />
    public override bool MoveToAttribute(string name) => _inner.MoveToAttribute(name);

    /// <inheritdoc />
    public override bool MoveToAttribute(string localName, string? namespaceURI) =>
        _inner.MoveToAttribute(localName, namespaceURI);

    /// <inheritdoc />
    public override void MoveToAttribute(int i) => _inner.MoveToAttribute(i);

    /// <inheritdoc />
    public override bool MoveToElement() => _inner.MoveToElement();

    /// <inheritdoc />
    public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();

    /// <inheritdoc />
    public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();

    /// <inheritdoc />
    public override bool Read()
    {
        var read = _inner.Read();
        if (read && _inner.NodeType == XmlNodeType.Element)
        {
            CaptureElementContext();
        }

        return read;
    }

    /// <inheritdoc />
    public override bool ReadAttributeValue() => _inner.ReadAttributeValue();

    /// <inheritdoc />
    public override int ReadValueChunk(char[] buffer, int index, int count) =>
        _inner.ReadValueChunk(buffer, index, count);

    /// <inheritdoc />
    public override void ResolveEntity() => _inner.ResolveEntity();

    private bool CanTranslateTypeValue =>
        _currentElementIsTextConstruct && !_currentElementHasBase64Mode;

    private static bool IsUnqualifiedTypeAttribute(string localName, string? namespaceURI) =>
        localName == "type" && string.IsNullOrEmpty(namespaceURI);

    private static bool IsTextConstruct(string localName)
    {
        return localName is "title" or "tagline" or "copyright" or "summary" or "content";
    }

    private static string TranslateElementName(string localName)
    {
        return localName switch
        {
            "tagline" => "subtitle",
            "issued" => "published",
            "modified" => "updated",
            "copyright" => "rights",
            _ => localName,
        };
    }

    private static string TranslateTypeValue(string value)
    {
        return value switch
        {
            "text/plain" => "text",
            "text/html" => "html",
            "application/xhtml+xml" => "xhtml",
            _ => value,
        };
    }

    private void CaptureElementContext()
    {
        _typeAttributeIndexes = null;
        _currentElementIsTextConstruct =
            _inner.NamespaceURI == NamespaceUri && IsTextConstruct(_inner.LocalName);
        _currentElementHasBase64Mode =
            string.Equals(_inner.GetAttribute("mode"), "base64", StringComparison.OrdinalIgnoreCase);

        if (CanTranslateTypeValue && _inner.MoveToFirstAttribute())
        {
            var index = 0;
            do
            {
                if (IsUnqualifiedTypeAttribute(_inner.LocalName, _inner.NamespaceURI))
                {
                    (_typeAttributeIndexes ??= []).Add(index);
                }

                index++;
            }
            while (_inner.MoveToNextAttribute());
            _inner.MoveToElement();
        }
    }
}
