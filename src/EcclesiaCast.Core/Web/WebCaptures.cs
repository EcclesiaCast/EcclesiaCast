using System.Globalization;

namespace EcclesiaCast.Core.Web;

/// <summary>
/// The photos taken of a web page: how they are named and which of them are
/// old enough to go. They pile up on their own (one per press of «Proyectar
/// foto»), so each start of the program sweeps the stale ones — except those
/// the operator kept by saving them in the media library.
/// </summary>
public static class WebCaptures
{
    /// <summary>How many days a photo is kept when nobody saved it.</summary>
    public const int KeepDays = 7;

    private const string Prefix = "web-";
    private const string Extension = ".png";

    /// <summary>«web-20261008-143005.png»: sorts by date and says when it was taken.</summary>
    public static string FileName(DateTime taken) =>
        Prefix + taken.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + Extension;

    /// <summary>
    /// The name for a photo taken now, never one that already exists: two
    /// presses in the same second must not write over the picture that is on
    /// the screen.
    /// </summary>
    public static string UniqueFileName(DateTime taken, Func<string, bool> exists)
    {
        var name = FileName(taken);
        var stem = name[..^Extension.Length];
        for (var n = 2; exists(name); n++)
            name = $"{stem}-{n}{Extension}";
        return name;
    }

    /// <summary>True for a file the program made as a photo (not something else dropped in its folder).</summary>
    public static bool IsCapture(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        return name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
               && name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The photos to delete: older than <see cref="KeepDays"/> days and not
    /// among the <paramref name="keep"/> paths (the ones saved in the library).
    /// </summary>
    public static IReadOnlyList<string> Expired(
        IEnumerable<(string Path, DateTime LastWrite)> files, DateTime now, IEnumerable<string?> keep)
    {
        var kept = new HashSet<string>(
            keep.Where(p => !string.IsNullOrWhiteSpace(p))!.Select(p => p!),
            StringComparer.OrdinalIgnoreCase);
        var limit = now - TimeSpan.FromDays(KeepDays);

        return files
            .Where(f => IsCapture(f.Path) && f.LastWrite < limit && !kept.Contains(f.Path))
            .Select(f => f.Path)
            .ToList();
    }
}
