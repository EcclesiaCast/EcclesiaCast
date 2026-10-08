using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Bible;

public class JsonBibleExporterTests
{
    private static ParsedBible Sample() => new(
    [
        new ParsedBook(1, "Génesis",
        [
            new ParsedVerse(1, 1, "En el principio creó Dios \"los cielos\" y la tierra."),
            new ParsedVerse(1, 2, "Y la tierra estaba desordenada."),
            new ParsedVerse(2, 1, "Fueron, pues, acabados los cielos."),
        ]),
        // A gap in the verses and a chapter with nothing in it.
        new ParsedBook(43, "Juan",
        [
            new ParsedVerse(1, 1, "En el principio era el Verbo."),
            new ParsedVerse(1, 3, "Todas las cosas por él fueron hechas."),
            new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo."),
        ]),
    ]);

    private static List<ParsedVerse> AllVerses(ParsedBible bible) =>
        bible.Books.OrderBy(b => b.Number)
            .SelectMany(b => b.Verses.Select(v => v with { Chapter = v.Chapter + b.Number * 1000 }))
            .ToList();

    [Fact]
    public void Exporting_and_importing_gives_back_the_same_verses()
    {
        var original = Sample();

        var back = JsonBibleImporter.Parse(JsonBibleExporter.ToJson(original));

        // Only the books that have verses come back from the importer's rows.
        var withVerses = back.Books.Where(b => b.Verses.Count > 0).ToList();
        Assert.Equal(original.Books.Select(b => (b.Number, b.Name)), withVerses.Select(b => (b.Number, b.Name)));
        Assert.Equal(AllVerses(original), AllVerses(new ParsedBible(withVerses)));
    }

    [Fact]
    public void Every_book_is_written_so_the_position_is_the_book_number()
    {
        var back = JsonBibleImporter.Parse(JsonBibleExporter.ToJson(Sample()));

        Assert.Equal(66, back.Books.Count);
        Assert.Equal(64, back.MissingBookNumbers.Count);
        Assert.Equal(43, back.Books[42].Number);
        Assert.Equal("Juan", back.Books[42].Name);
    }

    [Fact]
    public void Accents_stay_readable_in_the_file()
    {
        var json = JsonBibleExporter.ToJson(Sample());

        Assert.Contains("Génesis", json);
        Assert.Contains("él fueron", json);
    }

    [Fact]
    public void A_version_saved_in_the_database_exports_and_imports_to_the_same_thing()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var saved = bibles.Import("Reina Valera", "RVR1960", "es", Sample());

        var exported = JsonBibleExporter.ToJson(bibles.ReadVersion(saved.Id));
        var again = bibles.Import("Copia", "COP", "es", JsonBibleImporter.Parse(exported));

        Assert.Equal(saved.VerseCount, again.VerseCount);
        var a = bibles.ReadVersion(saved.Id);
        var b = bibles.ReadVersion(again.Id);
        Assert.Equal(AllVerses(a), AllVerses(b));
        Assert.Equal(a.Books.Select(x => x.Name), b.Books.Select(x => x.Name));
    }
}
