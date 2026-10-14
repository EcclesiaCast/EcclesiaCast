using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Core.Tests.Updates;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Bible;

public class BundledBibleImporterTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-bibles-{Guid.NewGuid():N}");
    private readonly TempDatabase _db = new();
    private readonly MemorySettings _settings = new();

    public BundledBibleImporterTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private static ParsedBible Sample() => new(
    [
        new ParsedBook(1, "Génesis", [new ParsedVerse(1, 1, "En el principio crió Dios los cielos y la tierra.")]),
        new ParsedBook(43, "Juan", [new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo.")]),
    ]);

    private void Leave(string fileName, params (string Name, string Abbreviation)[] versions)
    {
        using var stream = File.Create(Path.Combine(_folder, fileName));
        BiblePackage.Write(stream, versions.Select(v => (v.Name, v.Abbreviation, "es", Sample())));
    }

    [Fact]
    public void The_bibles_the_installer_left_are_brought_into_the_library()
    {
        Leave("a.ecbiblias", ("Reina-Valera 1909", "RV1909"));
        Leave("b.ecbiblias", ("Versión Biblia Libre", "VBL"));
        var bibles = new BibleRepository(_db.Path);

        var result = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Equal(["Reina-Valera 1909", "Versión Biblia Libre"], result.Imported);
        Assert.Equal(["RV1909", "VBL"], bibles.GetVersions().Select(v => v.Abbreviation).Order());
        Assert.All(bibles.GetVersions(), v => Assert.Equal(2, v.VerseCount));
    }

    [Fact]
    public void Starting_again_does_not_bring_them_twice()
    {
        Leave("a.ecbiblias", ("Reina-Valera 1909", "RV1909"));
        var bibles = new BibleRepository(_db.Path);
        BundledBibleImporter.Run(_folder, bibles, _settings);

        var second = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Empty(second.Imported);
        Assert.Single(bibles.GetVersions());
    }

    [Fact]
    public void A_bible_the_operator_deleted_does_not_come_back()
    {
        Leave("a.ecbiblias", ("Reina-Valera 1909", "RV1909"));
        var bibles = new BibleRepository(_db.Path);
        BundledBibleImporter.Run(_folder, bibles, _settings);
        bibles.DeleteVersion(bibles.GetVersions().Single().Id);

        var next = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Empty(next.Imported);
        Assert.Empty(bibles.GetVersions());
    }

    [Fact]
    public void A_bible_the_library_already_had_is_not_duplicated_nor_brought_back_after_deleting_it()
    {
        var bibles = new BibleRepository(_db.Path);
        var mine = bibles.Import("Mi Reina Valera", "rv 1909", "es", Sample());
        Leave("a.ecbiblias", ("Reina-Valera 1909", "RV1909"));

        var first = BundledBibleImporter.Run(_folder, bibles, _settings);
        bibles.DeleteVersion(mine.Id);
        var second = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Empty(first.Imported);
        Assert.Empty(second.Imported);
        Assert.Empty(bibles.GetVersions());
    }

    [Fact]
    public void A_file_that_is_not_a_package_is_reported_and_the_others_still_come_in()
    {
        File.WriteAllText(Path.Combine(_folder, "a-rota.ecbiblias"), "esto no es un zip");
        Leave("b.ecbiblias", ("Reina-Valera 1909", "RV1909"));
        var bibles = new BibleRepository(_db.Path);

        var result = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Equal(["a-rota.ecbiblias"], result.Failed);
        Assert.Equal(["Reina-Valera 1909"], result.Imported);
    }

    [Fact]
    public void Other_files_in_the_folder_are_ignored()
    {
        File.WriteAllText(Path.Combine(_folder, "notas.txt"), "hola");
        var bibles = new BibleRepository(_db.Path);

        var result = BundledBibleImporter.Run(_folder, bibles, _settings);

        Assert.Empty(result.Imported);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public void A_missing_folder_is_not_an_error()
    {
        var bibles = new BibleRepository(_db.Path);

        var result = BundledBibleImporter.Run(Path.Combine(_folder, "no-existe"), bibles, _settings);

        Assert.Empty(result.Imported);
        Assert.Empty(result.Failed);
    }
}
