// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using System.Text.RegularExpressions;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Estimates the reading time of an article from its HTML content.
/// </summary>
public static class ReadingTimeEstimator
{
    private const double WordsPerMinute = 200.0;

    private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns the localized reading time text for the given HTML content,
    /// or an empty string when the content is empty or the estimated reading
    /// time is one minute or less.
    /// </summary>
    /// <param name="contentHtml">The article content as HTML.</param>
    /// <returns>The formatted reading time, or an empty string.</returns>
    public static string EstimateText(string? contentHtml)
    {
        if (string.IsNullOrWhiteSpace(contentHtml))
        {
            return string.Empty;
        }

        var text = HtmlTagRegex.Replace(contentHtml, string.Empty);
        var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Max(1, (int)Math.Round(wordCount / WordsPerMinute));
        if (minutes <= 1)
        {
            return string.Empty;
        }

        return string.Format(CultureInfo.CurrentCulture, AppResources.ArticleReadingTimeFormat, minutes);
    }
}
