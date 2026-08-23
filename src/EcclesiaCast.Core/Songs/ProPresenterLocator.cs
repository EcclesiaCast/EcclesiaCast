namespace EcclesiaCast.Core.Songs;

/// <summary>One of ProPresenter's libraries: a folder full of <c>.pro</c> songs.</summary>
public sealed record ProPresenterLibrary(string Name, string Path, IReadOnlyList<string> SongFiles)
{
    public int SongCount => SongFiles.Count;
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

            libraries.Add(new ProPresenterLibrary(name, folder, ReadSongFiles(folder)));
        }

        return libraries;
    }

    /// <summary>Reads a single folder as a library (for a manually chosen one).</summary>
    public static ProPresenterLibrary ReadFolder(string folder) =>
        new(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), folder, ReadSongFiles(folder));

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
