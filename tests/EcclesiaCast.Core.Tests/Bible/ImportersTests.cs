using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Tests.Bible;

public class JsonBibleImporterTests
{
    [Fact]
    public void Parses_books_chapters_and_verses_in_array_order()
    {
        const string json = """
            [
              { "name": "Génesis", "chapters": [["En el principio", "La tierra estaba"]] },
              { "name": "Éxodo", "chapters": [["Estos son los nombres"], ["Y murió José"]] }
            ]
            """;

        var bible = JsonBibleImporter.Parse(json);

        Assert.Equal(2, bible.Books.Count);
        Assert.Equal(1, bible.Books[0].Number);
        Assert.Equal("Génesis", bible.Books[0].Name);
        Assert.Equal(2, bible.Books[0].Verses.Count);
        Assert.Equal(new ParsedVerse(1, 1, "En el principio"), bible.Books[0].Verses[0]);
        Assert.Equal(new ParsedVerse(1, 2, "La tierra estaba"), bible.Books[0].Verses[1]);

        Assert.Equal(2, bible.Books[1].Number);
        Assert.Equal(2, bible.Books[1].Verses.Count);
        Assert.Equal(new ParsedVerse(2, 1, "Y murió José"), bible.Books[1].Verses[1]);
    }

    [Fact]
    public void Missing_name_falls_back_to_the_catalog()
    {
        const string json = """[{ "chapters": [["Verso"]] }]""";

        var bible = JsonBibleImporter.Parse(json);

        Assert.Equal("Génesis", bible.Books[0].Name);
    }

    [Fact]
    public void Reports_missing_books_when_the_array_is_incomplete()
    {
        const string json = """[{ "name": "Génesis", "chapters": [["Uno"]] }]""";

        var bible = JsonBibleImporter.Parse(json);

        Assert.Equal(65, bible.MissingBookNumbers.Count);
        Assert.DoesNotContain(1, bible.MissingBookNumbers);
    }

    [Fact]
    public void Non_array_root_throws()
    {
        Assert.Throws<FormatException>(() => JsonBibleImporter.Parse("{}"));
    }
}

public class ZefaniaBibleImporterTests
{
    [Fact]
    public void Parses_books_by_bnumber_regardless_of_order()
    {
        const string xml = """
            <XMLBIBLE>
              <BIBLEBOOK bnumber="43" bname="Juan">
                <CHAPTER cnumber="3">
                  <VERS vnumber="16">Porque de tal manera amó Dios al mundo</VERS>
                  <VERS vnumber="17">Porque no envió Dios a su Hijo</VERS>
                </CHAPTER>
              </BIBLEBOOK>
              <BIBLEBOOK bnumber="1" bname="Génesis">
                <CHAPTER cnumber="1">
                  <VERS vnumber="1">En el principio</VERS>
                </CHAPTER>
              </BIBLEBOOK>
            </XMLBIBLE>
            """;

        var bible = ZefaniaBibleImporter.Parse(xml);

        Assert.Equal(2, bible.Books.Count);
        Assert.Equal(1, bible.Books[0].Number);
        Assert.Equal(43, bible.Books[1].Number);
        Assert.Equal(2, bible.Books[1].Verses.Count);
        Assert.Equal(new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo"), bible.Books[1].Verses[0]);
    }

    [Fact]
    public void Books_outside_1_to_66_are_ignored()
    {
        const string xml = """
            <XMLBIBLE>
              <BIBLEBOOK bnumber="99" bname="Apócrifo">
                <CHAPTER cnumber="1"><VERS vnumber="1">Texto</VERS></CHAPTER>
              </BIBLEBOOK>
            </XMLBIBLE>
            """;

        var bible = ZefaniaBibleImporter.Parse(xml);

        Assert.Empty(bible.Books);
    }

    [Fact]
    public void Missing_bname_falls_back_to_the_catalog()
    {
        const string xml = """
            <XMLBIBLE>
              <BIBLEBOOK bnumber="19">
                <CHAPTER cnumber="23"><VERS vnumber="1">El Señor es mi pastor</VERS></CHAPTER>
              </BIBLEBOOK>
            </XMLBIBLE>
            """;

        var bible = ZefaniaBibleImporter.Parse(xml);

        Assert.Equal("Salmos", bible.Books[0].Name);
    }
}
