using System.Text.Json;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class BibleImportHelpTests
{
    private static ParsedBible Sample => BibleImportHelp.SampleBible();

    [Fact]
    public void The_json_example_is_read_back_by_the_importer_as_the_same_bible()
    {
        var back = JsonBibleImporter.Parse(BibleImportHelp.ExampleJson());

        Assert.Equal(Sample.VerseCount, back.VerseCount);
        var john = back.Books.Single(b => b.Number == 43);
        Assert.Equal("Porque de tal manera amó Dios al mundo, que ha dado á su Hijo unigénito, para que todo aquel que en él creyere, no se pierda, mas tenga vida eterna.",
            john.Verses.Single(v => v.Chapter == 3 && v.Verse == 16).Text);
    }

    [Fact]
    public void The_zefania_example_is_read_back_by_the_importer_as_the_same_bible()
    {
        var back = ZefaniaBibleImporter.Parse(BibleImportHelp.ExampleZefaniaXml());

        Assert.Equal(Sample.VerseCount, back.VerseCount);
        Assert.Equal(["Génesis", "Juan"], back.Books.Select(b => b.Name).OrderBy(n => n, StringComparer.Ordinal));
        var genesis = back.Books.Single(b => b.Number == 1);
        Assert.Equal("En el principio crió Dios los cielos y la tierra.", genesis.Verses.Single(v => v.Verse == 1).Text);
    }

    [Fact]
    public void Both_examples_say_the_same_thing()
    {
        var json = JsonBibleImporter.Parse(BibleImportHelp.ExampleJson());
        var xml = ZefaniaBibleImporter.Parse(BibleImportHelp.ExampleZefaniaXml());

        static string Flat(ParsedBible bible) => string.Join("|", bible.Books.Where(b => b.Verses.Count > 0)
            .OrderBy(b => b.Number).SelectMany(b => b.Verses.Select(v => $"{b.Number}:{v.Chapter}:{v.Verse}:{v.Text}")));

        Assert.Equal(Flat(json), Flat(xml));
    }

    [Fact]
    public void The_json_example_has_the_66_books_the_importer_needs_by_position()
    {
        using var doc = JsonDocument.Parse(BibleImportHelp.ExampleJson());

        Assert.Equal(66, doc.RootElement.GetArrayLength());
        Assert.Equal("Génesis", doc.RootElement[0].GetProperty("name").GetString());
        Assert.Equal("Juan", doc.RootElement[42].GetProperty("name").GetString());
    }


    [Fact]
    public void The_xml_example_keeps_accents_readable_instead_of_numeric_codes()
    {
        var xml = BibleImportHelp.ExampleZefaniaXml();

        Assert.Contains("crió", xml);
        Assert.Contains("Génesis", xml);
        Assert.DoesNotContain("&#", xml);
    }
    [Fact]
    public void The_xml_example_is_well_formed()
    {
        Assert.NotNull(System.Xml.Linq.XDocument.Parse(BibleImportHelp.ExampleZefaniaXml()));
    }

    [Fact]
    public void The_preview_is_the_start_of_the_real_example_and_says_it_goes_on()
    {
        var preview = BibleImportHelp.JsonPreview();

        var expected = BibleImportHelp.ExampleJson().Replace(Environment.NewLine, "\n")[..20];
        Assert.StartsWith(expected, preview);
        Assert.Contains("66", preview);
        Assert.True(preview.Split('\n').Length < 20);
    }

    [Fact]
    public void The_instructions_for_an_ai_say_what_the_importer_needs()
    {
        var text = BibleImportHelp.InstructionsForAi();

        Assert.Contains("66", text);
        Assert.Contains("\"name\"", text);
        Assert.Contains("\"chapters\"", text);
        Assert.Contains("Zefania", text);
        Assert.Contains("Python", text);
        Assert.Contains("UTF-8", text);
    }

    [Fact]
    public void The_shape_described_in_the_instructions_is_the_one_the_importer_reads()
    {
        // The instructions' example with its «...» filled in.
        const string shaped = """[{ "name": "Génesis", "chapters": [["uno", "dos"], ["tres"]] }, { "name": "Éxodo", "chapters": [] }]""";

        var bible = JsonBibleImporter.Parse(shaped);

        Assert.Equal(3, bible.VerseCount);
        Assert.Equal(["Génesis", "Éxodo"], bible.Books.Select(b => b.Name));
    }
}
