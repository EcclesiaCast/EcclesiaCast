using System.IO.Compression;
using System.Text;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Bible;

public class BiblePackageTests
{
    private static ParsedBible Sample(string firstVerse) => new(
    [
        new ParsedBook(1, "Génesis", [new ParsedVerse(1, 1, firstVerse), new ParsedVerse(1, 2, "Y la tierra estaba desordenada.")]),
        new ParsedBook(43, "Juan", [new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo.")]),
    ]);

    private static MemoryStream TwoVersionPackage()
    {
        var stream = new MemoryStream();
        BiblePackage.Write(stream,
        [
            ("Reina-Valera 1909", "RV1909", "es", Sample("En el principio crió Dios los cielos y la tierra.")),
            ("Versión Biblia Libre", "VBL", "es", Sample("Al principio, Dios creó los cielos y la tierra.")),
        ]);
        stream.Position = 0;
        return stream;
    }

    /// <summary>A zip made by hand, to feed the reader things Write would never produce.</summary>
    private static MemoryStream HandMade(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void The_manifest_lists_every_version_with_its_name_abbreviation_and_language()
    {
        using var package = TwoVersionPackage();

        var entries = BiblePackage.ReadManifest(package);

        Assert.Equal(["RV1909", "VBL"], entries.Select(e => e.Abbreviation));
        Assert.Equal(["Reina-Valera 1909", "Versión Biblia Libre"], entries.Select(e => e.Name));
        Assert.All(entries, e => Assert.Equal("es", e.Language));
    }

    [Fact]
    public void A_version_read_back_has_the_verses_that_were_written()
    {
        using var package = TwoVersionPackage();
        var entries = BiblePackage.ReadManifest(package);
        package.Position = 0;

        var bible = BiblePackage.ReadVersion(package, entries[1]);

        Assert.Equal(3, bible.VerseCount);
        var genesis = bible.Books.Single(b => b.Number == 1);
        Assert.Equal("Al principio, Dios creó los cielos y la tierra.", genesis.Verses[0].Text);
        Assert.Equal("Porque de tal manera amó Dios al mundo.", bible.Books.Single(b => b.Number == 43).Verses.Single().Text);
    }

    [Fact]
    public void A_zip_without_a_manifest_is_not_a_package()
    {
        using var zip = HandMade(("otro.txt", "hola"));

        Assert.Throws<FormatException>(() => BiblePackage.ReadManifest(zip));
    }

    [Fact]
    public void A_file_that_is_not_a_zip_is_not_a_package()
    {
        using var notZip = new MemoryStream(Encoding.UTF8.GetBytes("esto no es un zip, ni cerca"));

        Assert.Throws<FormatException>(() => BiblePackage.ReadManifest(notZip));
    }

    [Fact]
    public void A_broken_manifest_is_reported_as_a_bad_package()
    {
        using var zip = HandMade((BiblePackage.ManifestName, "{ esto no es json"));

        Assert.Throws<FormatException>(() => BiblePackage.ReadManifest(zip));
    }

    [Fact]
    public void Entries_with_missing_data_or_pointing_to_nothing_are_left_out()
    {
        using var zip = HandMade(
            (BiblePackage.ManifestName, """
                { "format": 1, "versions": [
                  { "file": "a.json", "name": "Buena", "abbreviation": "BUE" },
                  { "file": "a.json", "name": "Sin abreviatura", "abbreviation": "  " },
                  { "file": "no-esta.json", "name": "Sin archivo", "abbreviation": "NOA" },
                  { "file": "a.json", "name": "Repetida", "abbreviation": "bue" },
                  { "name": "Sin file", "abbreviation": "SIF" }
                ] }
                """),
            ("a.json", "[]"));

        var entries = BiblePackage.ReadManifest(zip);

        var only = Assert.Single(entries);
        Assert.Equal("BUE", only.Abbreviation);
        Assert.Equal("es", only.Language);
    }

    [Fact]
    public void A_version_file_with_a_path_in_its_name_is_only_looked_up_inside_the_zip()
    {
        // The name is never used to touch the disk: it is a key in the zip.
        using var zip = HandMade(
            (BiblePackage.ManifestName, """{ "versions": [ { "file": "..\\..\\x.json", "name": "Rara", "abbreviation": "RAR" } ] }"""));

        Assert.Empty(BiblePackage.ReadManifest(zip));
    }

    [Fact]
    public void A_version_that_is_not_a_bible_json_is_reported_as_damaged()
    {
        using var zip = HandMade(
            (BiblePackage.ManifestName, """{ "versions": [ { "file": "a.json", "name": "Rota", "abbreviation": "ROT" } ] }"""),
            ("a.json", "{ roto"));
        var entry = BiblePackage.ReadManifest(zip).Single();
        zip.Position = 0;

        Assert.Throws<FormatException>(() => BiblePackage.ReadVersion(zip, entry));
    }

    [Fact]
    public void An_entry_far_bigger_than_a_bible_is_refused_instead_of_read()
    {
        var huge = new string(' ', 70 * 1024 * 1024);
        using var zip = HandMade(
            (BiblePackage.ManifestName, """{ "versions": [ { "file": "a.json", "name": "Enorme", "abbreviation": "ENO" } ] }"""),
            ("a.json", huge));
        var entry = BiblePackage.ReadManifest(zip).Single();
        zip.Position = 0;

        Assert.Throws<FormatException>(() => BiblePackage.ReadVersion(zip, entry));
    }

    [Fact]
    public void A_package_with_no_versions_reads_as_empty()
    {
        using var package = new MemoryStream();
        BiblePackage.Write(package, []);
        package.Position = 0;

        Assert.Empty(BiblePackage.ReadManifest(package));
    }

    [Fact]
    public void Versions_saved_in_one_library_travel_in_a_package_to_another()
    {
        using var origin = new TempDatabase();
        using var destination = new TempDatabase();
        var from = new BibleRepository(origin.Path);
        var to = new BibleRepository(destination.Path);
        var a = from.Import("Reina Valera 1960", "RVR1960", "es", Sample("En el principio creó Dios los cielos y la tierra."));
        var b = from.Import("Nueva Versión", "NVI", "es", Sample("Dios, en el principio, creó los cielos y la tierra."));
        from.Import("No va", "XXX", "es", Sample("Esta no se elige."));

        // What the export command does: the ticked versions, read one by one.
        using var package = new MemoryStream();
        var chosen = new[] { a.Id, b.Id };
        var versions = from.GetVersions();
        BiblePackage.Write(package, chosen.Select(id =>
        {
            var info = versions.Single(v => v.Id == id);
            return (info.Name, info.Abbreviation, info.Language, from.ReadVersion(id));
        }));
        package.Position = 0;

        // What the import command does on the other computer.
        var offered = BiblePackage.ReadManifest(package);
        foreach (var choice in BibleInstallPlanner.Offer(offered, to.GetVersions()).Where(c => !c.AlreadyInstalled))
        {
            package.Position = 0;
            to.Import(choice.Entry.Name, choice.Entry.Abbreviation, choice.Entry.Language, BiblePackage.ReadVersion(package, choice.Entry));
        }

        Assert.Equal(["NVI", "RVR1960"], to.GetVersions().Select(v => v.Abbreviation).Order());
        Assert.All(to.GetVersions(), v => Assert.Equal(3, v.VerseCount));
        var again = BibleInstallPlanner.Offer(offered, to.GetVersions());
        Assert.All(again, c => Assert.True(c.AlreadyInstalled));
    }

    [Fact]
    public void Offering_marks_the_versions_the_library_already_has()
    {
        using var package = TwoVersionPackage();
        var entries = BiblePackage.ReadManifest(package);
        var installed = new[] { new BibleVersionInfo(1, "Reina Valera", "rv 1909", "es", 31102) };

        var choices = BibleInstallPlanner.Offer(entries, installed);

        Assert.Equal([true, false], choices.Select(c => c.AlreadyInstalled));
    }
}
