using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class LyricsTagsTests
{
    [Fact]
    public void The_first_verse_is_number_one()
    {
        Assert.Equal("Estrofa 1", LyricsTags.NextVerse(""));
        Assert.Equal("Estrofa 1", LyricsTags.NextVerse("Santo, santo\n\n[Coro]\nGloria"));
    }

    [Fact]
    public void The_next_verse_is_one_more_than_the_highest_in_the_text()
    {
        var lyrics = "[Estrofa 1]\nuno\n\n[Coro]\ndos\n\n[ estrofa 3 ]\ntres";

        Assert.Equal("Estrofa 4", LyricsTags.NextVerse(lyrics));
    }

    [Fact]
    public void A_verse_without_a_number_counts_as_the_first()
    {
        Assert.Equal("Estrofa 2", LyricsTags.NextVerse("[Estrofa]\r\nuno"));
    }

    [Fact]
    public void The_tag_goes_above_the_paragraph_the_cursor_is_in()
    {
        var lyrics = "Santo, santo\nes el Señor\n\nGloria a Dios\nen las alturas";
        var caret = lyrics.IndexOf("alturas", StringComparison.Ordinal);

        var edit = LyricsTags.Insert(lyrics, caret, LyricsTags.Chorus);
        var result = edit.ApplyTo(lyrics);

        Assert.Equal("Santo, santo\nes el Señor\n\n[Coro]\nGloria a Dios\nen las alturas", result);
        Assert.Equal(result.IndexOf("alturas", StringComparison.Ordinal), edit.Caret);
    }

    [Fact]
    public void The_tagged_paragraph_is_read_with_that_name_and_the_tag_is_not_projected()
    {
        var lyrics = "Santo, santo\r\n\r\nGloria a Dios";
        var first = LyricsTags.Insert(lyrics, 2, LyricsTags.Verse).ApplyTo(lyrics);
        var both = LyricsTags.Insert(first, first.Length, LyricsTags.Chorus).ApplyTo(first);

        Assert.Equal("[Estrofa 1]\r\nSanto, santo\r\n\r\n[Coro]\r\nGloria a Dios", both);
        var sections = LyricsParser.Parse(both);
        Assert.Equal(["Estrofa 1", "Coro"], sections.Select(s => s.Label));
        Assert.Equal(["Santo, santo", "Gloria a Dios"], sections.Select(s => s.Text));
    }

    [Fact]
    public void Verses_number_themselves_as_they_are_added()
    {
        var lyrics = "uno\n\ndos\n\ntres";
        lyrics = LyricsTags.Insert(lyrics, 0, LyricsTags.Verse).ApplyTo(lyrics);
        lyrics = LyricsTags.Insert(lyrics, lyrics.Length, LyricsTags.Verse).ApplyTo(lyrics);

        Assert.Equal("[Estrofa 1]\nuno\n\ndos\n\n[Estrofa 2]\ntres", lyrics);
    }

    [Fact]
    public void A_paragraph_that_already_has_a_tag_gets_it_replaced()
    {
        var lyrics = "[Estrofa 1]\nuno\n\n[Estrofa 2]\ndos";

        var asChorus = LyricsTags.Insert(lyrics, lyrics.Length, LyricsTags.Chorus);
        Assert.Equal("[Estrofa 1]\nuno\n\n[Coro]\ndos", asChorus.ApplyTo(lyrics));
        Assert.Equal(asChorus.ApplyTo(lyrics).Length, asChorus.Caret);

        // Asking for «Estrofa» again on a verse leaves its number alone.
        var again = LyricsTags.Insert(lyrics, lyrics.Length, LyricsTags.Verse);
        Assert.Equal(lyrics, again.ApplyTo(lyrics));
    }

    [Fact]
    public void On_a_blank_line_the_tag_goes_there_and_the_cursor_stays_below_it()
    {
        var lyrics = "uno\n\n";

        var edit = LyricsTags.Insert(lyrics, lyrics.Length, LyricsTags.Bridge);

        Assert.Equal("uno\n\n[Puente]\n", edit.ApplyTo(lyrics));
        Assert.Equal(edit.ApplyTo(lyrics).Length, edit.Caret);
    }

    [Fact]
    public void A_blank_line_is_left_before_the_tag_when_the_line_above_has_text()
    {
        var lyrics = "uno\n";

        var edit = LyricsTags.Insert(lyrics, lyrics.Length, LyricsTags.Ending);

        Assert.Equal("uno\n\n[Final]\n", edit.ApplyTo(lyrics));
    }

    [Fact]
    public void In_an_empty_box_the_tag_uses_the_line_break_it_is_given()
    {
        var edit = LyricsTags.Insert("", 0, LyricsTags.Verse, "\r\n");

        Assert.Equal("[Estrofa 1]\r\n", edit.ApplyTo(""));
        Assert.Equal(13, edit.Caret);
    }
}
