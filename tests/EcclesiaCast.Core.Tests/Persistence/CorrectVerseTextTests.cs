using EcclesiaCast.Core.Bible;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class CorrectVerseTextTests
{
    private static ParsedBible Sample(string verse8) => new(
    [
        new ParsedBook(43, "Juan",
        [
            new ParsedVerse(8, 10, "Mujer, ¿dónde están?"),
            new ParsedVerse(8, 11, verse8),
            new ParsedVerse(8, 12, "Yo soy la luz del mundo."),
            new ParsedVerse(9, 11, verse8),
        ]),
    ]);

    [Fact]
    public void A_correction_replaces_the_stored_text_of_that_verse_only()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var version = bibles.Import("Prueba", "PRU", "es", Sample("Vete y no peques más. ----------"));

        var changed = bibles.UpdateVerseText(version.Id, 43, 8, 11, 11, "Vete y no peques más.");

        Assert.Equal(1, changed);
        var passage = bibles.GetPassage(version.Id, new BibleReference(43, 8, null, null));
        Assert.Equal(
            ["Mujer, ¿dónde están?", "Vete y no peques más.", "Yo soy la luz del mundo."],
            passage.Select(v => v.Text));
        // Another chapter with the same words is another verse.
        Assert.Equal("Vete y no peques más. ----------",
            bibles.GetPassage(version.Id, new BibleReference(43, 9, 11, 11))[0].Text);
    }

    [Fact]
    public void A_group_of_verses_gets_the_same_correction_in_each()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var version = bibles.Import("Prueba", "PRU", "es", Sample("11-12 (12-13) Texto repetido."));

        var changed = bibles.UpdateVerseText(version.Id, 43, 8, 10, 12, "Texto corregido.");

        Assert.Equal(3, changed);
        Assert.All(bibles.GetPassage(version.Id, new BibleReference(43, 8, null, null)),
            v => Assert.Equal("Texto corregido.", v.Text));
    }

    [Fact]
    public void Another_version_is_not_touched()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var first = bibles.Import("Una", "UNA", "es", Sample("Original."));
        var second = bibles.Import("Otra", "OTR", "es", Sample("Original."));

        bibles.UpdateVerseText(first.Id, 43, 8, 11, 11, "Corregido.");

        Assert.Equal("Original.", bibles.GetPassage(second.Id, new BibleReference(43, 8, 11, 11))[0].Text);
    }
}
