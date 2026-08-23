using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class ProPresenterLocatorTests : IDisposable
{
    private readonly string _temp = Path.Combine(
        Path.GetTempPath(), "ecclesiacast-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_temp, recursive: true); } catch { /* best effort */ }
    }

    private string MakeLibrary(string root, string library, params string[] songs)
    {
        var folder = Path.Combine(_temp, root, "Libraries", library);
        Directory.CreateDirectory(folder);
        foreach (var song in songs)
            File.WriteAllText(Path.Combine(folder, song), "x");
        return folder;
    }

    [Fact]
    public void Finds_the_root_that_has_a_libraries_folder()
    {
        MakeLibrary("ProPresenter", "Default", "Una.pro");
        Directory.CreateDirectory(Path.Combine(_temp, "ProPresenter6")); // sin Libraries

        var roots = ProPresenterLocator.FindRoots([_temp]);

        Assert.Equal(Path.Combine(_temp, "ProPresenter"), Assert.Single(roots));
    }

    [Fact]
    public void Missing_base_folders_are_skipped()
    {
        var roots = ProPresenterLocator.FindRoots([Path.Combine(_temp, "no-existe"), string.Empty]);

        Assert.Empty(roots);
    }

    [Fact]
    public void The_same_root_reached_twice_is_listed_once()
    {
        MakeLibrary("ProPresenter", "Default", "Una.pro");

        var roots = ProPresenterLocator.FindRoots([_temp, _temp]);

        Assert.Single(roots);
    }

    [Fact]
    public void Reads_each_library_with_its_songs()
    {
        MakeLibrary("ProPresenter", "Canciones", "Una.pro", "Otra.pro");
        MakeLibrary("ProPresenter", "Biblia", "Salmo.pro");

        var libraries = ProPresenterLocator.ReadLibraries(Path.Combine(_temp, "ProPresenter"));

        Assert.Equal(2, libraries.Count);
        Assert.Equal("Biblia", libraries[0].Name);
        Assert.Equal(1, libraries[0].SongCount);
        Assert.Equal(2, libraries[1].SongCount);
    }

    [Fact]
    public void ProPresenters_own_index_files_are_not_libraries()
    {
        MakeLibrary("ProPresenter", "Canciones", "Una.pro");
        MakeLibrary("ProPresenter", "LibraryData");
        MakeLibrary("ProPresenter", "Library");

        var libraries = ProPresenterLocator.ReadLibraries(Path.Combine(_temp, "ProPresenter"));

        Assert.Equal("Canciones", Assert.Single(libraries).Name);
    }

    [Fact]
    public void Files_that_are_not_pro_are_ignored()
    {
        var folder = MakeLibrary("ProPresenter", "Canciones", "Una.pro", "notas.txt", "vieja.pro6");

        var library = ProPresenterLocator.ReadFolder(folder);

        Assert.Equal("Canciones", library.Name);
        Assert.EndsWith("Una.pro", Assert.Single(library.SongFiles));
    }

    [Fact]
    public void An_empty_library_is_still_listed()
    {
        MakeLibrary("ProPresenter", "Vacía");

        var library = Assert.Single(ProPresenterLocator.ReadLibraries(Path.Combine(_temp, "ProPresenter")));

        Assert.Equal(0, library.SongCount);
    }

    [Fact]
    public void A_root_without_libraries_reads_as_empty()
    {
        Assert.Empty(ProPresenterLocator.ReadLibraries(Path.Combine(_temp, "no-existe")));
    }
}
