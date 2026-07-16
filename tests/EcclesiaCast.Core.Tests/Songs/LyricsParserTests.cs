using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class LyricsParserTests
{
    [Fact]
    public void Every_line_becomes_one_slide()
    {
        const string lyrics = """
            Grande es tu fidelidad
            oh Dios mi Padre
            no hay sombra de variación
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(3, sections.Count);
        Assert.Equal("Grande es tu fidelidad", sections[0].Text);
        Assert.Equal("oh Dios mi Padre", sections[1].Text);
        Assert.Equal(new[] { 0, 1, 2 }, sections.Select(s => s.Order));
    }

    [Fact]
    public void Blank_lines_are_ignored()
    {
        const string lyrics = """
            Línea uno


            Línea dos
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
    }

    [Fact]
    public void Unlabeled_slides_are_numbered()
    {
        var sections = LyricsParser.Parse("Uno\nDos");

        Assert.Equal("1", sections[0].Label);
        Assert.Equal("2", sections[1].Label);
    }

    [Fact]
    public void A_tag_line_labels_the_following_slides_and_is_not_a_slide()
    {
        const string lyrics = """
            [Coro]
            Grande es tu fidelidad
            cada mañana veo tu amor
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.All(sections, s => Assert.Equal("Coro", s.Label));
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
    public void Empty_or_whitespace_lyrics_produce_no_sections()
    {
        Assert.Empty(LyricsParser.Parse(null));
        Assert.Empty(LyricsParser.Parse(""));
        Assert.Empty(LyricsParser.Parse("   \n\n  "));
    }

    [Fact]
    public void Handles_windows_line_endings()
    {
        var sections = LyricsParser.Parse("Línea uno\r\nLínea dos");

        Assert.Equal(2, sections.Count);
        Assert.Equal("Línea uno", sections[0].Text);
    }

    [Fact]
    public void Round_trips_through_editor_text()
    {
        const string lyrics = """
            Sin etiqueta
            [Coro]
            Con etiqueta uno
            Con etiqueta dos
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
