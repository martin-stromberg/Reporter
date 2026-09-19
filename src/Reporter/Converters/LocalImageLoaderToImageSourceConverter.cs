// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;

namespace Reporter.Converters;

/// <summary>
/// Converts an <c>ItemListItem.LocalImageLoader</c> delegate into a
/// <see cref="StreamImageSource"/> so the stored image blob is only fetched
/// when the platform image handler actually requests the stream.
/// </summary>
public class LocalImageLoaderToImageSourceConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Func<CancellationToken, Task<Stream>> loader
            ? ImageSource.FromStream(loader)
            : null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
