namespace EcclesiaCast.Core.Songs;

/// <summary>One of ProPresenter's libraries: a folder full of <c>.pro</c> songs.</summary>
/// <param name="CloudOnlyCount">
/// How many of those files only exist in the cloud. OneDrive's "Files
/// On-Demand" leaves placeholders on disk that look like normal files —
/// right size, right name — but reading one fails unless OneDrive is running
/// and downloads it first. A church library synced to OneDrive is the normal
/// case, so this is counted up front instead of failing song by song.
/// </param>
public sealed record ProPresenterLibrary(
    string Name,
    string Path,
    IReadOnlyList<string> SongFiles,
    int CloudOnlyCount = 0)
{
    public int SongCount => SongFiles.Count;

    /// <summary>True when none of the files can be read without downloading first.</summary>
    public bool IsFullyCloudOnly => SongCount > 0 && CloudOnlyCount == SongCount;
}

/// <summary>
/// Finds where ProPresenter keeps its songs. It stores them as plain folders
/// under a document root — one folder per library — and that root sits either
/// in Documents or, very often, in the OneDrive copy of Documents, which is
/// why several bases are probed rather than a single hard-coded path.
/// </summary>
public static class ProPresenterLocator
{
    /// <summary>Root folder names used by ProPresenter 6 and 7.</summary>
    private static readonly string[] RootNames = ["ProPresenter", "ProPresenter7", "ProPresenter6"];

    /// <summary>Not libraries: ProPresenter drops its own index files beside them.</summary>
    private static readonly string[] ReservedNames = ["Library", "LibraryData"];

    /// <summary>
    /// The ProPresenter roots that actually exist under the given base folders
    /// (Documents, the OneDrive Documents, anything else worth probing).
    /// </summary>
    public static IReadOnlyList<string> FindRoots(IEnumerable<string> baseFolders)
    {
        var found = new List<string>();

        foreach (var folder in baseFolders)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                continue;

            foreach (var name in RootNames)
            {
                var root = Path.Combine(folder, name);
                // The same folder can be reached two ways (Documents and the
                // OneDrive mirror of it), so keep only one of each.
                if (Directory.Exists(Path.Combine(root, "Libraries"))
                    && !found.Contains(root, StringComparer.OrdinalIgnoreCase))
                    found.Add(root);
            }
        }

        return found;
    }

    /// <summary>
    /// The libraries under a ProPresenter root, with the song files in each.
    /// Empty libraries are kept: seeing "0 canciones" beats silently hiding a
    /// folder the operator expected to find.
    /// </summary>
    public static IReadOnlyList<ProPresenterLibrary> ReadLibraries(string root)
    {
        var librariesFolder = Path.Combine(root, "Libraries");
        if (!Directory.Exists(librariesFolder))
            return [];

        var libraries = new List<ProPresenterLibrary>();

        foreach (var folder in Directory.EnumerateDirectories(librariesFolder).OrderBy(f => f))
        {
            var name = Path.GetFileName(folder);
            if (ReservedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            var files = ReadSongFiles(folder);
            libraries.Add(new ProPresenterLibrary(name, folder, files, CountCloudOnly(files)));
        }

        return libraries;
    }

    /// <summary>Reads a single folder as a library (for a manually chosen one).</summary>
    public static ProPresenterLibrary ReadFolder(string folder)
    {
        var files = ReadSongFiles(folder);
        return new ProPresenterLibrary(
            Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)),
            folder,
            files,
            CountCloudOnly(files));
    }

    /// <summary>
    /// True when the file is a cloud placeholder that has to be downloaded
    /// before it can be read. Windows marks these with Offline and, for
    /// OneDrive's on-demand files, RecallOnDataAccess.
    /// </summary>
    public static bool IsCloudOnly(string path)
    {
        // Not in the FileAttributes enum, so the documented value is used.
        const FileAttributes RecallOnDataAccess = (FileAttributes)0x400000;

        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & (FileAttributes.Offline | RecallOnDataAccess)) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static int CountCloudOnly(IReadOnlyList<string> files) => files.Count(IsCloudOnly);

    private static IReadOnlyList<string> ReadSongFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder, "*.pro", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
