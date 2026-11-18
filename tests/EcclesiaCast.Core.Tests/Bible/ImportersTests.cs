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

public class YouVersionStyleJsonBibleImporterTests
{
    [Fact]
    public void Parses_verses_by_usfm_code_skipping_non_verse_items()
    {
        const string json = """
            {
              "books": [
                {
                  "book_usfm": "JHN",
                  "name": "Juan",
                  "chapters": [
                    {
                      "chapter_usfm": "JHN.3",
                      "items": [
                        { "type": "heading1", "verse_numbers": [], "lines": ["Un encuentro de noche"] },
                        { "type": "verse", "verse_numbers": [16], "lines": ["Porque de tal manera amó Dios al mundo,", "que ha dado a su Hijo unigénito."] },
                        { "type": "verse", "verse_numbers": [17], "lines": ["Porque no envió Dios a su Hijo para condenar al mundo."] }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var bible = JsonBibleImporter.Parse(json);

        var john = Assert.Single(bible.Books);
        Assert.Equal(43, john.Number);
        Assert.Equal("Juan", john.Name);
        Assert.Equal(2, john.Verses.Count);
        Assert.Equal(new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito."), john.Verses[0]);
        Assert.Equal(new ParsedVerse(3, 17, "Porque no envió Dios a su Hijo para condenar al mundo."), john.Verses[1]);
    }

    [Fact]
    public void Books_with_unrecognized_usfm_codes_are_skipped()
    {
        const string json = """
            { "books": [ { "book_usfm": "INTRO", "name": "Introducción", "chapters": [] } ] }
            """;

        var bible = JsonBibleImporter.Parse(json);

        Assert.Empty(bible.Books);
    }

    [Fact]
    public void A_verse_covering_multiple_numbers_is_stored_under_each()
    {
        const string json = """
            {
              "books": [
                {
                  "book_usfm": "GEN",
                  "chapters": [
                    {
                      "chapter_usfm": "GEN.1",
                      "items": [
                        { "type": "verse", "verse_numbers": [1, 2], "lines": ["Texto combinado"] }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var bible = JsonBibleImporter.Parse(json);

        Assert.Equal(2, bible.Books[0].Verses.Count);
        Assert.All(bible.Books[0].Verses, v => Assert.Equal("Texto combinado", v.Text));
    }

    [Fact]
    public void Objects_without_a_books_array_throw()
    {
        Assert.Throws<FormatException>(() => JsonBibleImporter.Parse("""{ "version_id": 1 }"""));
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

public class OpenSongBibleImporterTests
{
    [Fact]
    public void Parses_books_by_name_when_the_file_does_not_have_all_66()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <bible>
              <b n="Génesis">
                <c n="1">
                  <v n="1">En el principio</v>
                  <v n="2">Y la tierra estaba desordenada</v>
                </c>
              </b>
              <b n="Juan">
                <c n="3">
                  <v n="16">Porque de tal manera amó Dios al mundo</v>
                </c>
              </b>
            </bible>
            """;

        var bible = OpenSongBibleImporter.Parse(xml);

        Assert.Equal([1, 43], bible.Books.Select(b => b.Number));
        Assert.Equal("Juan", bible.Books[1].Name);
        Assert.Equal(new ParsedVerse(3, 16, "Porque de tal manera amó Dios al mundo"), bible.Books[1].Verses[0]);
        Assert.Equal(3, bible.VerseCount);
    }

    [Fact]
    public void A_full_bible_with_english_names_goes_by_position()
    {
        var english = new[] { "Genesis", "Exodus" }.Concat(Enumerable.Range(3, 64).Select(n => $"Book {n}")).ToList();
        english[42] = "John";
        var xml = "<bible>" + string.Concat(english.Select(n => $"<b n=\"{n}\"><c n=\"1\"><v n=\"1\">{n} 1:1</v></c></b>")) + "</bible>";

        var bible = OpenSongBibleImporter.Parse(xml);

        Assert.Equal(66, bible.Books.Count);
        var john = bible.Books.Single(b => b.Number == 43);
        Assert.Equal("John", john.Name);
        Assert.Equal("John 1:1", john.Verses[0].Text);
    }

    [Fact]
    public void A_new_testament_alone_starts_at_mateo()
    {
        var names = new[] { "Matthew", "Mark", "Luke", "John" }.Concat(Enumerable.Range(5, 23).Select(n => $"NT {n}"));
        var xml = "<bible>" + string.Concat(names.Select(n => $"<b n=\"{n}\"><c n=\"1\"><v n=\"1\">x</v></c></b>")) + "</bible>";

        var bible = OpenSongBibleImporter.Parse(xml);

        Assert.Equal(40, bible.Books[0].Number);
        Assert.Equal(43, bible.Books.Single(b => b.Name == "John").Number);
        Assert.Equal(66, bible.Books[^1].Number);
    }

    [Fact]
    public void Reads_the_beblia_spelling_with_testaments_and_book_numbers()
    {
        const string xml = """
            <bible translation="Ejemplo">
              <testament name="Old">
                <book number="19">
                  <chapter number="23">
                    <verse number="1">Jehová es mi pastor; nada me faltará.</verse>
                  </chapter>
                </book>
              </testament>
              <testament name="New">
                <book number="43">
                  <chapter number="3">
                    <verse number="16">Porque de tal manera amó Dios al mundo</verse>
                  </chapter>
                </book>
              </testament>
            </bible>
            """;

        var bible = OpenSongBibleImporter.Parse(xml);

        Assert.Equal([19, 43], bible.Books.Select(b => b.Number));
        Assert.Equal("Salmos", bible.Books[0].Name);
        Assert.Equal(new ParsedVerse(23, 1, "Jehová es mi pastor; nada me faltará."), bible.Books[0].Verses[0]);
    }

    [Fact]
    public void A_joined_verse_counts_as_its_first_number_and_bold_inside_a_verse_is_not_a_book()
    {
        const string xml = """
            <bible>
              <b n="Juan">
                <c n="1">
                  <v n="1-2">En el principio era el <b>Verbo</b></v>
                  <v n="3">Todas las cosas</v>
                  <v n="4"> </v>
                </c>
              </b>
            </bible>
            """;

        var bible = OpenSongBibleImporter.Parse(xml);

        var john = Assert.Single(bible.Books);
        Assert.Equal([1, 3], john.Verses.Select(v => v.Verse));
        Assert.Equal("En el principio era el Verbo", john.Verses[0].Text);
    }
}

public class XmlBibleImporterTests
{
    [Fact]
    public void Tells_zefania_from_opensong()
    {
        var zefania = XmlBibleImporter.Parse("""<XMLBIBLE><BIBLEBOOK bnumber="43"><CHAPTER cnumber="3"><VERS vnumber="16">Zefania</VERS></CHAPTER></BIBLEBOOK></XMLBIBLE>""");
        var openSong = XmlBibleImporter.Parse("""<bible><b n="Juan"><c n="3"><v n="16">OpenSong</v></c></b></bible>""");

        Assert.Equal("Zefania", zefania.Books.Single(b => b.Number == 43).Verses[0].Text);
        Assert.Equal("OpenSong", openSong.Books.Single(b => b.Number == 43).Verses[0].Text);
    }

    [Fact]
    public void Another_xml_says_which_formats_are_read()
    {
        var error = Assert.Throws<FormatException>(() => XmlBibleImporter.Parse("<songs><song/></songs>"));

        Assert.Contains("<songs>", error.Message);
        Assert.Contains("OpenSong", error.Message);
    }
}
