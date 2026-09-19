// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text.RegularExpressions;
using Reporter.Core.Models;

namespace Reporter.Core.Services;

/// <summary>
/// Sanitizes article HTML for display in the article detail WebView.
/// </summary>
public static class ArticleHtmlSanitizer
{
    private static readonly Regex ScriptTagRegex = new Regex("<script[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex IframeTagRegex = new Regex("<iframe[^>]*>.*?</iframe>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex StyleTagRegex = new Regex("<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ObjectTagRegex = new Regex("<object[^>]*>.*?</object>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EmbedTagRegex = new Regex("<embed[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex FormTagRegex = new Regex("<form[^>]*>.*?</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DangerousElementRegex = new Regex("<(applet|audio|video)[^>]*>.*?</\\1[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DangerousTagsRegex = new Regex("</?(link|meta|base|applet|audio|video)[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex OnEventRegex = new Regex("on\\w+\\s*=\\s*(?:\"[^\"]*\"|'[^']*'|[^\\s>]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex JavaScriptRegex = new Regex("javascript:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AnchorWithContentRegex = new Regex("<a\\b[^>]*>(.*?)</a\\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AnchorTagRegex = new Regex("</?a\\b[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ImageTagRegex = new Regex("<img\\b[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Performs a best-effort regex-based HTML sanitization.
    /// This is not a bullet-proof replacement for a dedicated HTML sanitizer library.
    /// </summary>
    /// <param name="html">The raw HTML to sanitize.</param>
    /// <param name="forOffline">
    /// When <c>true</c>, anchor tags are replaced by their inner text and image
    /// elements are removed so that no external resources are required to read
    /// the article without a network connection.
    /// </param>
    /// <param name="localImage">
    /// The locally stored article image. When <paramref name="forOffline"/> is
    /// <c>true</c> and an image is supplied, the first <c>&lt;img&gt;</c> tag is
    /// replaced by an <c>&lt;img&gt;</c> with a <c>data:</c> URI — or, when the
    /// content holds no image tag (including empty or <c>null</c> content), a
    /// header image is prepended — while all other image tags are removed.
    /// </param>
    /// <returns>The sanitized HTML or <c>null</c> if the input is <c>null</c>.</returns>
    public static string? Sanitize(string? html, bool forOffline, ItemImage? localImage = null)
    {
        // Das lokale Bild wird vor der Whitespace-Fruehrueckkehr ausgewertet,
        // damit bild-only-Artikel ohne ContentHtml ein Header-Bild-Fragment
        // erhalten statt auf den leeren Inhalt zurueckzufallen.
        var localImageTag = forOffline && localImage is not null
            ? CreateLocalImageTag(localImage)
            : null;

        if (string.IsNullOrWhiteSpace(html))
        {
            return localImageTag ?? html;
        }

        html = ScriptTagRegex.Replace(html, string.Empty);
        html = IframeTagRegex.Replace(html, string.Empty);
        html = StyleTagRegex.Replace(html, string.Empty);
        html = ObjectTagRegex.Replace(html, string.Empty);
        html = EmbedTagRegex.Replace(html, string.Empty);
        html = FormTagRegex.Replace(html, string.Empty);
        html = DangerousElementRegex.Replace(html, string.Empty);
        html = DangerousTagsRegex.Replace(html, string.Empty);
        html = OnEventRegex.Replace(html, string.Empty);
        html = JavaScriptRegex.Replace(html, string.Empty);

        if (forOffline)
        {
            html = AnchorWithContentRegex.Replace(html, "$1");
            html = AnchorTagRegex.Replace(html, string.Empty);

            if (localImageTag is not null)
            {
                var replaced = false;
                html = ImageTagRegex.Replace(html, match =>
                {
                    if (replaced)
                    {
                        return string.Empty;
                    }

                    replaced = true;
                    return localImageTag;
                });
                if (!replaced)
                {
                    html = localImageTag + html;
                }
            }
            else
            {
                html = ImageTagRegex.Replace(html, string.Empty);
            }
        }

        return html;
    }

    private static string CreateLocalImageTag(ItemImage image)
    {
        var mediaType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/png" : image.ContentType;
        var base64 = Convert.ToBase64String(image.Data);
        return $"<img src=\"data:{mediaType};base64,{base64}\" />";
    }
}
