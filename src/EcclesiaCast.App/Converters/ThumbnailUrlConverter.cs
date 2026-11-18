using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Converters;

/// <summary>
/// The address of a small picture to the picture itself, read from the
/// internet in the background and decoded at card size.
/// </summary>
public sealed class ThumbnailUrlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string { Length: > 0 } url)
            return null;

        var thumb = new BitmapImage();
        thumb.BeginInit();
        thumb.UriSource = new Uri(url);
        thumb.DecodePixelWidth = 320;
        thumb.CacheOption = BitmapCacheOption.OnLoad;
        thumb.EndInit();
        return thumb;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
