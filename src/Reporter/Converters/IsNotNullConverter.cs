// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;

namespace Reporter.Converters;

/// <summary>
/// Converts a value to <c>true</c> when it is not <c>null</c>.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Nur für One-Way-Bindungen gedacht: Rückrichtung lässt den Quellwert unverändert.
        return Binding.DoNothing;
    }
}
