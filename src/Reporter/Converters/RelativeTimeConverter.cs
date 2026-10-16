// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Services;

namespace Reporter.Converters;

/// <summary>
/// Converts a nullable <see cref="DateTime"/> into a short relative time text via <see cref="RelativeTimeFormatter"/>.
/// An optional string <c>ConverterParameter</c> is prepended (e.g. a separator) only when a time is shown.
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = RelativeTimeFormatter.Format(value as DateTime?, DateTime.UtcNow);
        return text.Length > 0 && parameter is string prefix ? prefix + text : text;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Nur für One-Way-Bindungen gedacht: Rückrichtung lässt den Quellwert unverändert.
        return Binding.DoNothing;
    }
}
