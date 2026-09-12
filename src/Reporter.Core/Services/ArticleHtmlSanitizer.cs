// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Text.RegularExpressions;

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
    /// <returns>The sanitized HTML or <c>null</c> if the input is <c>null</c>.</returns>
    public static string? Sanitize(string? html, bool forOffline)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
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
            html = ImageTagRegex.Replace(html, string.Empty);
        }

        return html;
    }
}
