using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class LyricsSearchTests
{
    private static readonly string[] Slides =
    [
        "Ardiendo en fuego mi alma está,\nardiendo en fuego mi alma está.",
        "¡Oh Señor!\nquiero que ardas en mi ser.",
        "Gloriosa llama me limpiará,\n¡Oh aleluya! mi alma ardiendo está.",
    ];

    [Fact]
    public void Finds_every_slide_with_the_words_in_order()
    {
        Assert.Equal([0, 2], LyricsSearch.Matches(Slides, "mi alma"));
    }

    [Fact]
    public void Accents_capitals_and_punctuation_do_not_count()
    {
        Assert.Equal([1], LyricsSearch.Matches(Slides, "oh senor"));
        Assert.Equal([2], LyricsSearch.Matches(Slides, "ALELUYA"));
    }

    [Fact]
    public void A_phrase_that_runs_over_two_lines_is_found()
    {
        Assert.Equal([0], LyricsSearch.Matches(Slides, "está ardiendo en fuego"));
    }

    [Fact]
    public void Nothing_typed_finds_nothing()
    {
        Assert.Empty(LyricsSearch.Matches(Slides, "   "));
        Assert.Empty(LyricsSearch.Matches(Slides, null));
        Assert.Empty(LyricsSearch.Matches(Slides, "no está"));
    }
}
