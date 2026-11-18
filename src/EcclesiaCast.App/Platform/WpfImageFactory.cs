using System.IO;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Platform;

/// <summary>
/// Frozen WPF bitmaps: read whole on creation, so the file is not held, and
/// usable from any thread.
/// </summary>
public sealed class WpfImageFactory : IImageFactory
{
    public object? FromFile(string path, int? decodeWidth = null)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            if (decodeWidth is { } width)
                bitmap.DecodePixelWidth = width;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object? FromBytes(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public (int Width, int Height)? ReadSize(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or IOException or ArgumentException)
        {
            return null;
        }
    }
}
