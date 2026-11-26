using Avalonia.Media.Imaging;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The pictures the projector and the previews draw, read once and kept by
/// weak reference: a background that comes back, a poster drawn in several
/// views, the fill of every slide. A file rewritten under the same name (a
/// page drawn again) is read again: its date is part of the key. What the
/// WPF side splits in <c>ImageLoadCache</c> and <c>PreviewBitmaps</c>.
///
/// Remote pictures (YouTube posters) are not read here: YouTube does not
/// exist on a Mac, and on Windows the Avalonia side has no use for them yet.
/// </summary>
public static class Pictures
{
    private static readonly Dictionary<(string Path, long Stamp, int Width), WeakReference<Bitmap>> Cache = [];
    private static readonly object Sync = new();

    /// <summary>The date of the file, or 0 when it is not a local file that exists.</summary>
    public static long StampOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || IsRemote(path))
            return 0;

        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.LastWriteTimeUtc.Ticks : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool IsRemote(string path) => path.StartsWith("http", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The width to read a background at: the 1920 it always was, or the
    /// whole file when it is zoomed in (it would go soft otherwise).
    /// </summary>
    public static int DecodeWidthFor(MediaItem media) =>
        ImageDecodeSize.NeedsFileSize(media.Zoom) ? 0 : ImageDecodeSize.Baseline;

    /// <summary>Already read, or null; never reads.</summary>
    public static Bitmap? Cached(string? path, int width)
    {
        var stamp = StampOf(path);
        if (stamp == 0)
            return null;

        lock (Sync)
            return Cache.TryGetValue((path!, stamp, width), out var weak) && weak.TryGetTarget(out var bitmap) ? bitmap : null;
    }

    /// <summary>
    /// The picture at <paramref name="path"/>, read at <paramref name="width"/>
    /// (0: as big as the file, but never past <see cref="ImageDecodeSize.Max"/>).
    /// Null when it is missing or unreadable. Safe from any thread.
    /// </summary>
    public static Bitmap? Load(string? path, int width = ImageDecodeSize.Baseline)
    {
        var stamp = StampOf(path);
        if (stamp == 0)
            return null;

        var key = (path!, stamp, width);
        lock (Sync)
        {
            if (Cache.TryGetValue(key, out var weak) && weak.TryGetTarget(out var cached))
                return cached;
        }

        try
        {
            Bitmap bitmap;
            using (var stream = File.OpenRead(path!))
                bitmap = width > 0 ? Bitmap.DecodeToWidth(stream, width) : new Bitmap(stream);

            if (width == 0 && bitmap.PixelSize.Width > ImageDecodeSize.Max)
            {
                using var stream = File.OpenRead(path!);
                bitmap = Bitmap.DecodeToWidth(stream, ImageDecodeSize.Max);
            }

            lock (Sync)
            {
                if (Cache.Count > 600)
                    Prune();
                Cache[key] = new WeakReference<Bitmap>(bitmap);
            }
            return bitmap;
        }
        catch
        {
            // Unreadable: whoever asked draws its fallback.
            return null;
        }
    }

    private static void Prune()
    {
        foreach (var dead in Cache.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToList())
            Cache.Remove(dead);
    }
}
