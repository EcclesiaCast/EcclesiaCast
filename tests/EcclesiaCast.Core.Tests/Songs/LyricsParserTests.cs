using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class LyricsParserTests
{
    [Fact]
    public void Parses_tagged_sections_with_labels_in_order()
    {
        const string lyrics = """
            [Verso 1]
            Grande es tu fidelidad
            oh Dios mi Padre

            [Coro]
            Grande es tu fidelidad
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.Equal("Verso 1", sections[0].Label);
        Assert.Equal("Grande es tu fidelidad\noh Dios mi Padre", sections[0].Text);
        Assert.Equal("Coro", sections[1].Label);
        Assert.Equal(0, sections[0].Order);
        Assert.Equal(1, sections[1].Order);
    }

    [Fact]
    public void Untagged_lyrics_split_on_blank_lines_as_numbered_verses()
    {
        const string lyrics = """
            Primera estrofa
            segunda línea

            Segunda estrofa
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.Equal("Verso 1", sections[0].Label);
        Assert.Equal("Verso 2", sections[1].Label);
    }

    [Fact]
    public void Single_untagged_block_is_labeled_Verso()
    {
        var sections = LyricsParser.Parse("Sublime gracia del Señor");

        var section = Assert.Single(sections);
        Assert.Equal("Verso", section.Label);
    }

    [Fact]
    public void Text_before_the_first_tag_becomes_a_Verso_section()
    {
        const string lyrics = """
            Intro sin etiqueta

            [Coro]
            El coro
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.Equal("Verso", sections[0].Label);
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
    public void Repeated_labels_are_allowed()
    {
        const string lyrics = """
            [Coro]
            Uno

            [Coro]
            Dos
            """;

        var sections = LyricsParser.Parse(lyrics);

        Assert.Equal(2, sections.Count);
        Assert.All(sections, s => Assert.Equal("Coro", s.Label));
    }

    [Fact]
    public void Handles_windows_line_endings()
    {
        var sections = LyricsParser.Parse("[Coro]\r\nLínea uno\r\nLínea dos");

        var section = Assert.Single(sections);
        Assert.Equal("Línea uno\nLínea dos", section.Text);
    }

    [Fact]
    public void Round_trips_through_tagged_text()
    {
        const string lyrics = """
            [Verso 1]
            Uno

            [Coro]
            Dos
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
