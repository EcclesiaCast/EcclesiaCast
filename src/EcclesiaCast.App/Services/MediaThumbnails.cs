using System.IO;
using System.Windows.Media.Imaging;
using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using Serilog;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Builds the poster image shown for a media item in the library, and keeps
/// the generated PNGs in the app's data folder.
/// </summary>
public static class MediaThumbnails
{
    public const int Width = 320;
    public const int Height = 180;

    public static string Directory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcclesiaCast", "thumbnails");

    /// <summary>
    /// Returns the thumbnail path for a media file: the shell's own image when
    /// it has one, otherwise a frame decoded with VLC. Images fall back to the
    /// file itself. Returns null when nothing could be produced.
    /// </summary>
    public static string? Create(string sourcePath, MediaType type, LibVLC? videoEngine)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        var source = ShellThumbnail.TryGet(sourcePath, Width, Height);

        if (source is null && type == MediaType.Video && videoEngine is not null)
            source = VideoThumbnail.TryGet(videoEngine, sourcePath, Width, Height);

        return source is not null
            ? SavePng(source)
            : type == MediaType.Image ? sourcePath : null;
    }

    /// <summary>Deletes a generated thumbnail; ignores the media's own file.</summary>
    public static void Delete(string? thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            return;

        try
        {
            // Only ever remove PNGs we generated into our own folder.
            if (Path.GetDirectoryName(thumbnailPath)?.Equals(Directory, StringComparison.OrdinalIgnoreCase) == true
                && File.Exists(thumbnailPath))
                File.Delete(thumbnailPath);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo borrar la miniatura {Path}", thumbnailPath);
        }
    }

    /// <summary>
    /// Removes generated PNGs no longer referenced by any media item. Files WPF
    /// still has open (a poster currently on screen) simply fail to delete and
    /// get picked up on a later run.
    /// </summary>
    public static void DeleteOrphans(IEnumerable<string?> referenced)
    {
        try
        {
            if (!System.IO.Directory.Exists(Directory))
                return;

            var inUse = new HashSet<string>(
                referenced.Where(p => !string.IsNullOrWhiteSpace(p))!,
                StringComparer.OrdinalIgnoreCase);

            foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*.png"))
            {
                if (inUse.Contains(file))
                    continue;
                try { File.Delete(file); }
                catch (IOException) { /* still open in the UI; next run gets it */ }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudieron limpiar las miniaturas huérfanas");
        }
    }

    private static string? SavePng(BitmapSource source)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var target = Path.Combine(Directory, $"{Guid.NewGuid():N}.png");

            using var stream = File.Create(target);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            return target;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo guardar la miniatura");
            return null;
        }
    }
}
