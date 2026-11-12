// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Formats timestamps as short, human-friendly relative times ("5 min ago", "yesterday")
/// for list metadata. Older timestamps fall back to a compact calendar date.
/// </summary>
public static class RelativeTimeFormatter
{
    private const int DaysShownRelative = 7;

    /// <summary>
    /// Formats the given timestamp relative to <paramref name="nowUtc"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp to format. <see cref="DateTimeKind.Unspecified"/> values are treated as UTC, matching the persisted item dates.</param>
    /// <param name="nowUtc">The current point in time in UTC.</param>
    /// <param name="timeZone">The time zone used for calendar comparisons; defaults to <see cref="TimeZoneInfo.Local"/>.</param>
    /// <returns>The localized relative time, or an empty string when <paramref name="timestamp"/> is <c>null</c>.</returns>
    public static string Format(DateTime? timestamp, DateTime nowUtc, TimeZoneInfo? timeZone = null)
    {
        if (timestamp is null)
        {
            return string.Empty;
        }

        var culture = CultureInfo.CurrentCulture;
        var zone = timeZone ?? TimeZoneInfo.Local;
        var valueUtc = ToUtc(timestamp.Value);
        nowUtc = ToUtc(nowUtc);
        var delta = nowUtc - valueUtc;

        if (delta < TimeSpan.FromMinutes(1))
        {
            return AppResources.RelativeTimeJustNow;
        }

        if (delta < TimeSpan.FromHours(1))
        {
            return string.Format(culture, AppResources.RelativeTimeMinutes, (int)delta.TotalMinutes);
        }

        if (delta < TimeSpan.FromHours(24))
        {
            return string.Format(culture, AppResources.RelativeTimeHours, (int)delta.TotalHours);
        }

        var localValue = TimeZoneInfo.ConvertTimeFromUtc(valueUtc, zone);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone);
        var days = (localNow.Date - localValue.Date).Days;

        if (days <= 1)
        {
            return AppResources.RelativeTimeYesterday;
        }

        if (days < DaysShownRelative)
        {
            return string.Format(culture, AppResources.RelativeTimeDays, days);
        }

        return localValue.Year == localNow.Year
            ? localValue.ToString(ShortMonthDayPattern(culture), culture)
            : localValue.ToString("d", culture);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private static string ShortMonthDayPattern(CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.MonthDayPattern;
        return pattern.Contains("MMMM", StringComparison.Ordinal)
            ? pattern.Replace("MMMM", "MMM", StringComparison.Ordinal)
            : pattern;
    }
}
