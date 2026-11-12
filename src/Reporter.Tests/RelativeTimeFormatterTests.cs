// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Resources.Strings;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="RelativeTimeFormatter"/> class.
/// </summary>
public class RelativeTimeFormatterTests
{
    private static readonly DateTime NowUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Verifies that a missing timestamp yields an empty string.
    /// </summary>
    [Fact]
    public void Format_WithNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, RelativeTimeFormatter.Format(null, NowUtc, TimeZoneInfo.Utc));
    }

    /// <summary>
    /// Verifies that timestamps within the last minute (or in the future) read "just now".
    /// </summary>
    /// <param name="secondsAgo">The offset to the current time in seconds.</param>
    [Theory]
    [InlineData(30)]
    [InlineData(-120)]
    public void Format_WithinLastMinuteOrFuture_ReturnsJustNow(int secondsAgo)
    {
        var result = RelativeTimeFormatter.Format(NowUtc.AddSeconds(-secondsAgo), NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(AppResources.RelativeTimeJustNow, result);
    }

    /// <summary>
    /// Verifies that timestamps within the last hour are formatted in minutes.
    /// </summary>
    [Fact]
    public void Format_WithinLastHour_ReturnsMinutes()
    {
        var result = RelativeTimeFormatter.Format(NowUtc.AddMinutes(-25), NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, AppResources.RelativeTimeMinutes, 25), result);
    }

    /// <summary>
    /// Verifies that timestamps within the last day are formatted in hours.
    /// </summary>
    [Fact]
    public void Format_WithinLastDay_ReturnsHours()
    {
        var result = RelativeTimeFormatter.Format(NowUtc.AddHours(-3).AddMinutes(-10), NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, AppResources.RelativeTimeHours, 3), result);
    }

    /// <summary>
    /// Verifies that a timestamp on the previous calendar day reads "yesterday".
    /// </summary>
    [Fact]
    public void Format_PreviousCalendarDay_ReturnsYesterday()
    {
        var result = RelativeTimeFormatter.Format(NowUtc.AddHours(-30), NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(AppResources.RelativeTimeYesterday, result);
    }

    /// <summary>
    /// Verifies that timestamps within the last week are formatted in days.
    /// </summary>
    [Fact]
    public void Format_WithinLastWeek_ReturnsDays()
    {
        var result = RelativeTimeFormatter.Format(NowUtc.AddDays(-4), NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, AppResources.RelativeTimeDays, 4), result);
    }

    /// <summary>
    /// Verifies that older timestamps of the same year fall back to a short month-day date.
    /// </summary>
    [Fact]
    public void Format_OlderThanWeekSameYear_ReturnsMonthDay()
    {
        var timestamp = new DateTime(2026, 3, 14, 9, 0, 0, DateTimeKind.Utc);

        var result = RelativeTimeFormatter.Format(timestamp, NowUtc, TimeZoneInfo.Utc);

        Assert.Contains("14", result);
        Assert.DoesNotContain("2026", result);
    }

    /// <summary>
    /// Verifies that timestamps of a previous year include the year.
    /// </summary>
    [Fact]
    public void Format_PreviousYear_ReturnsShortDate()
    {
        var timestamp = new DateTime(2025, 3, 14, 9, 0, 0, DateTimeKind.Utc);

        var result = RelativeTimeFormatter.Format(timestamp, NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(timestamp.ToString("d", CultureInfo.CurrentCulture), result);
    }

    /// <summary>
    /// Verifies that unspecified-kind timestamps (as read from SQLite) are treated as UTC.
    /// </summary>
    [Fact]
    public void Format_WithUnspecifiedKind_TreatsValueAsUtc()
    {
        var unspecified = DateTime.SpecifyKind(NowUtc.AddMinutes(-5), DateTimeKind.Unspecified);

        var result = RelativeTimeFormatter.Format(unspecified, NowUtc, TimeZoneInfo.Utc);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, AppResources.RelativeTimeMinutes, 5), result);
    }
}
