// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ReadingTimeEstimator"/> class.
/// </summary>
public class ReadingTimeEstimatorTests
{
    /// <summary>
    /// Verifies that null content produces an empty reading time text.
    /// </summary>
    [Fact]
    public void EstimateText_NullContent_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText(null));
    }

    /// <summary>
    /// Verifies that empty content produces an empty reading time text.
    /// </summary>
    [Fact]
    public void EstimateText_EmptyContent_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText(string.Empty));
        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText("   "));
    }

    /// <summary>
    /// Verifies that short content is estimated at a minimum of one minute.
    /// </summary>
    [Fact]
    public void EstimateText_ShortContent_ReturnsOneMinute()
    {
        var result = ReadingTimeEstimator.EstimateText("Hello world");

        Assert.Equal(Format(1), result);
    }

    /// <summary>
    /// Verifies that HTML tags are stripped before counting words.
    /// </summary>
    [Fact]
    public void EstimateText_HtmlContent_StripsTags()
    {
        var result = ReadingTimeEstimator.EstimateText("<p>Hello <strong>world</strong></p>");

        Assert.Equal(Format(1), result);
    }

    /// <summary>
    /// Verifies that the word count is converted to minutes at 200 words per minute.
    /// </summary>
    [Fact]
    public void EstimateText_LongContent_ReturnsRoundedMinutes()
    {
        var content = string.Join(' ', Enumerable.Repeat("word", 400));

        var result = ReadingTimeEstimator.EstimateText(content);

        Assert.Equal(Format(2), result);
    }

    private static string Format(int minutes)
    {
        return string.Format(CultureInfo.CurrentCulture, AppResources.ArticleReadingTimeFormat, minutes);
    }
}
