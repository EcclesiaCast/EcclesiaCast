using System.IO.Compression;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// A pack of backgrounds: a .zip with looping videos (and pictures) that the
/// operator adds from «Archivo → Agregar paquete de fondos…». It is how more
/// backgrounds can come in without making the installer bigger.
/// </summary>
public static class BackgroundPack
{
    /// <summary>The tab the backgrounds of a pack go to, apart from the sample ones and the church's own.</summary>
    public const string Tab = "Fondos agregados";

    /// <summary>The most a pack may take once unzipped (a sample pack is about 30 MB).</summary>
    public const long MaxUnpackedBytes = 1L << 31;

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mov", ".webm", ".jpg", ".jpeg", ".png", ".webp",
    };

    public static bool IsBackground(string fileName) => Allowed.Contains(Path.GetExtension(fileName));

    /// <summary>
    /// Unzips the backgrounds of the pack into <paramref name="destination"/>,
    /// flat: only the file name of each entry is used (an entry called
    /// «..\..\x.mp4» cannot leave the folder), anything that is not a video or
    /// a picture is left out, and a name that is already taken gets « (2)».
    /// </summary>
    /// <returns>The files written, in the order of the pack.</returns>
    /// <exception cref="InvalidDataException">The file is not a zip, or it is too big.</exception>
    public static IReadOnlyList<string> Extract(string zipPath, string destination)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var wanted = zip.Entries.Where(e => e.Name.Length > 0 && IsBackground(e.Name)).ToList();
        if (wanted.Sum(e => e.Length) > MaxUnpackedBytes)
            throw new InvalidDataException("El paquete es demasiado grande.");

        Directory.CreateDirectory(destination);
        var written = new List<string>();
        foreach (var entry in wanted)
        {
            var target = FreeName(destination, Path.GetFileName(entry.Name));
            entry.ExtractToFile(target, overwrite: false);
            written.Add(target);
        }

        return written;
    }

    private static string FreeName(string folder, string fileName)
    {
        var path = Path.Combine(folder, fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var n = 2; File.Exists(path); n++)
            path = Path.Combine(folder, $"{stem} ({n}){ext}");
        return path;
    }
}
