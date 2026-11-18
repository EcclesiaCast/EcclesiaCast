namespace EcclesiaCast.Core.IO;

/// <summary>
/// Paths that may have been written on another system. A library made on
/// Windows carries «C:\Videos\a.mp4»; on a Mac, <see cref="Path"/> does not
/// see «\» as a separator and would take that whole text as a file name.
/// These helpers treat both bars as separators, so the same saved path can be
/// understood anywhere (for example to tell which media went missing).
/// </summary>
public static class PortablePath
{
    /// <summary>The path with both «\» and «/» changed to this system's separator.</summary>
    public static string Normalize(string path) =>
        string.IsNullOrEmpty(path)
            ? path
            : path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    /// <summary>
    /// The file name at the end of a path written with either kind of bar
    /// («C:\Videos\a.mp4» and «/Users/x/a.mp4» both give «a.mp4»). Empty when the
    /// path ends in a bar, like <see cref="Path.GetFileName(string)"/>.
    /// </summary>
    public static string FileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path[(path.LastIndexOfAny(['\\', '/']) + 1)..];
    }

    /// <summary>
    /// True when both paths point to the same place, ignoring upper and lower
    /// case, which bar is used, and a bar at the end. It only compares the
    /// text: it does not look at the disk.
    /// </summary>
    public static bool SamePath(string? first, string? second)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
            return false;

        return string.Equals(Key(first), Key(second), StringComparison.OrdinalIgnoreCase);
    }

    private static string Key(string path)
    {
        var normalized = Normalize(path);
        // A path made only of bars (the root «/») keeps one; any other loses its last bar.
        var trimmed = normalized.TrimEnd(Path.DirectorySeparatorChar);
        return trimmed.Length == 0 ? normalized[..1] : trimmed;
    }
}
