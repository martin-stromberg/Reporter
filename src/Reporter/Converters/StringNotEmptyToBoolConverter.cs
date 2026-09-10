using System.Globalization;

namespace Reporter.Converters;

/// <summary>
/// Converts a string value to a boolean indicating whether the string is not null or whitespace.
/// </summary>
public class StringNotEmptyToBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter?.ToString() ?? string.Empty : string.Empty;
    }
}
