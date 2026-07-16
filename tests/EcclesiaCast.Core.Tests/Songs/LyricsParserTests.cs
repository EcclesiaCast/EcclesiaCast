using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class LyricsParserTests
{
    [Fact]
    public void Each_paragraph_becomes_one_slide()
    {
        const string lyrics = """
            Grande es tu fidelidad
            oh Dios mi Padre

            No hay sombra de variación
            tu compasión no falla
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.Equal("Grande es tu fidelidad\noh Dios mi Padre", sections[0].Text);
        Assert.Equal("No hay sombra de variación\ntu compasión no falla", sections[1].Text);
    }

    [Fact]
    public void Line_breaks_inside_a_paragraph_are_preserved()
    {
        var sections = LyricsParser.Parse("Línea uno\nLínea dos\nLínea tres");

        var section = Assert.Single(sections);
        Assert.Equal("Línea uno\nLínea dos\nLínea tres", section.Text);
    }

    [Fact]
    public void Multiple_blank_lines_count_as_one_separator()
    {
        var sections = LyricsParser.Parse("Uno\n\n\n\nDos");

        Assert.Equal(2, sections.Count);
    }

    [Fact]
    public void Unlabeled_slides_are_numbered()
    {
        var sections = LyricsParser.Parse("Uno\n\nDos");

        Assert.Equal("1", sections[0].Label);
        Assert.Equal("2", sections[1].Label);
    }

    [Fact]
    public void A_tag_line_labels_the_following_paragraphs_and_is_not_a_slide()
    {
        const string lyrics = """
            [Coro]
            Grande es tu fidelidad
            cada mañana veo tu amor
            """;

        var sections = LyricsParser.Parse(lyrics);

        var section = Assert.Single(sections);
        Assert.Equal("Coro", section.Label);
        Assert.Equal("Grande es tu fidelidad\ncada mañana veo tu amor", section.Text);
    }

    [Fact]
    public void Labels_switch_when_a_new_tag_appears()
    {
        const string lyrics = """
            [Verso 1]
            Uno

            [Coro]
            Dos
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal("Verso 1", sections[0].Label);
        Assert.Equal("Coro", sections[1].Label);
    }

    [Fact]
    public void A_tag_also_splits_paragraphs_even_without_a_blank_line()
    {
        const string lyrics = """
            Uno
            [Coro]
            Dos
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.Equal("1", sections[0].Label);
        Assert.Equal("Coro", sections[1].Label);
    }

    [Fact]
    public void Empty_or_whitespace_lyrics_produce_no_sections()
    {
        Assert.Empty(LyricsParser.Parse(null));
        Assert.Empty(LyricsParser.Parse(""));
        Assert.Empty(LyricsParser.Parse("   \n\n  "));
    }

    [Fact]
    public void Handles_windows_line_endings()
    {
        var sections = LyricsParser.Parse("Línea uno\r\n\r\nLínea dos");

        Assert.Equal(2, sections.Count);
    }

    [Fact]
    public void Round_trips_through_editor_text()
    {
        const string lyrics = """
            Primer párrafo
            con dos líneas

            [Coro]
            El coro entero
            también de dos
            """;

        var parsed = LyricsParser.Parse(lyrics);
        var reparsed = LyricsParser.Parse(LyricsParser.ToTaggedText(parsed));

        Assert.Equal(parsed.Count, reparsed.Count);
        for (var i = 0; i < parsed.Count; i++)
        {
            Assert.Equal(parsed[i].Label, reparsed[i].Label);
            Assert.Equal(parsed[i].Text, reparsed[i].Text);
        }
    }
}
