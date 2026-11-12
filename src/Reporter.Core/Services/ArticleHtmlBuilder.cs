// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;

namespace Reporter.Core.Services;

/// <summary>
/// Composes the complete HTML document shown in the article detail WebView
/// from already sanitized article content, the item tags and the base font
/// size.
/// </summary>
public static class ArticleHtmlBuilder
{
    /// <summary>
    /// Builds the full HTML document (doctype, <c>&lt;head&gt;</c> with inline
    /// CSS, <c>&lt;body&gt;</c>) around the sanitized
    /// <paramref name="content"/>. When <paramref name="tags"/> is not empty a
    /// <c>&lt;p class='tags'&gt;</c> line with the HTML-encoded,
    /// comma-separated terms is appended after the content inside the body.
    /// </summary>
    /// <param name="content">The sanitized article HTML.</param>
    /// <param name="tags">The tag terms rendered below the article text.</param>
    /// <param name="baseFontSize">The base font size in pixels.</param>
    /// <returns>The complete HTML document.</returns>
    public static string Build(string content, IReadOnlyList<string> tags, int baseFontSize)
    {
        // The block is appended after ArticleHtmlSanitizer ran, so every term
        // must be encoded — feed tags are unchecked feed content.
        var tagBlock = tags.Count == 0
            ? string.Empty
            : $"<p class='tags'>{string.Join(", ", tags.Select(WebUtility.HtmlEncode))}</p>";

        return $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<meta name='color-scheme' content='light dark'>
<meta http-equiv='Content-Security-Policy' content=""default-src 'none'; style-src 'unsafe-inline'; img-src * data: blob:; font-src 'none'; script-src 'none';"" >
<style>
:root {{ color-scheme: light dark; }}
body {{
    margin: 16px;
    font-family: 'Newsreader', Georgia, 'Times New Roman', serif;
    font-size: {baseFontSize}px;
    line-height: 1.6;
    color: #0f172a;
    background-color: #ffffff;
}}
@media (prefers-color-scheme: dark) {{
    body {{
        color: #f1f5f9;
        background-color: #0f131c;
    }}
    a {{ color: #4edea3; }}
    p.tags {{ color: #94a3b8; }}
}}
h1, h2, h3, h4, h5, h6 {{
    font-family: 'Newsreader', Georgia, serif;
    line-height: 1.25;
}}
p {{ margin: 0 0 1em 0; }}
p.tags {{ color: #64748b; }}
img {{ max-width: 100%; height: auto; border-radius: 8px; }}
a {{ color: #0051d5; }}
blockquote {{
    border-left: 4px solid currentColor;
    padding-left: 16px;
    margin: 1em 0;
    opacity: 0.9;
}}
</style>
</head>
<body>{content}{tagBlock}</body>
</html>";
    }
}
