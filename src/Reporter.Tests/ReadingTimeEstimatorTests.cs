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
    /// Verifies that content estimated at one minute or less produces an empty
    /// reading time text — the label is hidden for very short articles.
    /// </summary>
    [Fact]
    public void EstimateText_OneMinuteContent_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText("Hello world"));
        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText(string.Join(' ', Enumerable.Repeat("word", 200))));
    }

    /// <summary>
    /// Verifies that content whose word count rounds down to zero minutes still
    /// produces an empty reading time text.
    /// </summary>
    /// <remarks>
    /// Regression note for the removed <c>Math.Max(1, …)</c> clamp in
    /// <see cref="ReadingTimeEstimator.EstimateText"/>: the clamp was dead code
    /// because the <c>minutes &lt;= 1</c> early return already covers a zero
    /// result. No red test was possible — the removal is behavior-neutral —
    /// so this test documents the zero-minute boundary instead.
    /// </remarks>
    [Fact]
    public void EstimateText_SubMinuteContent_ReturnsEmpty()
    {
        var content = string.Join(' ', Enumerable.Repeat("word", 50));

        Assert.Equal(string.Empty, ReadingTimeEstimator.EstimateText(content));
    }

    /// <summary>
    /// Verifies that content estimated at two minutes produces a reading time text.
    /// </summary>
    [Fact]
    public void EstimateText_TwoMinuteContent_ReturnsText()
    {
        var content = string.Join(' ', Enumerable.Repeat("word", 400));

        var result = ReadingTimeEstimator.EstimateText(content);

        Assert.Equal(Format(2), result);
    }

    /// <summary>
    /// Verifies that HTML tags are stripped before counting words.
    /// </summary>
    [Fact]
    public void EstimateText_HtmlContent_StripsTags()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 400));
        var result = ReadingTimeEstimator.EstimateText($"<p><strong>{words}</strong></p>");

        Assert.Equal(Format(2), result);
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
