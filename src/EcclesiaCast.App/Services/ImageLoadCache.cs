using System.IO;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Media;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The pictures the projector has read lately, ready to draw. Reading a big
/// picture (a 4K page, a poster) takes 60 to 130 ms; done in the middle of a
/// change of background it freezes the window and the new picture arrives
/// late. Here it is done on another thread — and ahead of time, when the
/// mouse rests on a card — so the change itself finds it waiting.
/// </summary>
public static class ImageLoadCache
{
    private const int Capacity = 8;

    private readonly record struct Key(string Path, int Width, long Stamp);

    private static readonly object Gate = new();
    private static readonly LinkedList<(Key Key, BitmapImage Image)> Entries = new();

    /// <summary>Last write of the file, in ticks: a page drawn again keeps its name but not its picture.</summary>
    public static long StampOf(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path).Ticks;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    /// <summary>The picture if it has been read already at this width, else null.</summary>
    public static BitmapImage? Get(string path, int width, long stamp)
    {
        lock (Gate)
        {
            for (var node = Entries.First; node is not null; node = node.Next)
            {
                if (node.Value.Key != new Key(path, width, stamp))
                    continue;

                Entries.Remove(node);
                Entries.AddFirst(node);
                return node.Value.Image;
            }
        }

        return null;
    }

    /// <summary>The most recent read of this file at any width, or null.</summary>
    public static BitmapImage? GetLatest(string path)
    {
        lock (Gate)
        {
            foreach (var entry in Entries)
            {
                if (entry.Key.Path == path)
                    return entry.Image;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the picture now, on the calling thread (any: the result is frozen),
    /// and keeps it. Never wider than the file itself: asking WPF for more
    /// than the file has only makes it enlarge the picture, which costs time
    /// and memory and adds nothing. Null when the file cannot be read.
    /// </summary>
    public static BitmapImage? Prepare(string path, int width, long stamp)
    {
        if (Get(path, width, stamp) is { } ready)
            return ready;

        try
        {
            var natural = NaturalWidth(path);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            // Our own cache is keyed by the file's date; WPF's is by name only.
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            if (width > 0 && (natural == 0 || natural > width))
                bitmap.DecodePixelWidth = width;
            bitmap.EndInit();
            bitmap.Freeze();

            lock (Gate)
            {
                Entries.AddFirst((new Key(path, width, stamp), bitmap));
                while (Entries.Count > Capacity)
                    Entries.RemoveLast();
            }

            return bitmap;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException
                                       or UriFormatException or UnauthorizedAccessException or ArgumentException)
        {
            Log.Debug(ex, "No se pudo leer la imagen {Path}", path);
            return null;
        }
    }

    /// <summary>Width of the file read from its header, 0 when that fails.</summary>
    private static int NaturalWidth(string path)
    {
        try
        {
            return BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None)
                .Frames[0].PixelWidth;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException
                                       or UriFormatException or ArgumentException)
        {
            return 0;
        }
    }

    /// <summary>
    /// The width to read this image at: the screen's, unless the media is
    /// zoomed in, in which case it also depends on the file's own size.
    /// </summary>
    public static int DecodeWidthFor(MediaItem media, string? path)
    {
        if (!ImageDecodeSize.NeedsFileSize(media.Zoom) || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return ImageDecodeSize.Baseline;

        try
        {
            // Only the header is read: no pixels are decoded to learn the size.
            var frame = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None)
                .Frames[0];
            return ImageDecodeSize.For(
                media.Scaling, media.Zoom, media.FrameWidth, media.FrameHeight, frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return ImageDecodeSize.Baseline;
        }
    }

    /// <summary>
    /// Reads a picture ahead of time, in the background, because the operator is
    /// about to project it. Does nothing for what is not a local image.
    /// </summary>
    public static Task PrefetchAsync(MediaItem media)
    {
        if (media.Type != MediaType.Image || string.IsNullOrWhiteSpace(media.Path)
            || media.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var path = media.Path;
        return Task.Run(() =>
        {
            if (!File.Exists(path))
                return;

            Prepare(path, DecodeWidthFor(media, path), StampOf(path));
        });
    }
}
