using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class SongTitlesTests
{
    [Theory]
    [InlineData("Mi Gozo", "mi gozo")]
    [InlineData("  MI   GOZO ", "mi gozo")]
    [InlineData("Mi Gozó", "mi gozo")]
    [InlineData("Él es Señor", "el es senor")]
    public void The_same_title_written_differently_counts_as_the_same(string typed, string stored)
    {
        Assert.True(SongTitles.Same(typed, stored));
    }

    [Fact]
    public void Different_titles_are_not_the_same()
    {
        Assert.False(SongTitles.Same("Mi Gozo", "Mi Gozo 2"));
        Assert.False(SongTitles.Same("Mi Gozo", "Mi Gozo (1)"));
    }

    [Fact]
    public void An_empty_title_is_never_the_same_as_another_empty_one()
    {
        Assert.False(SongTitles.Same("", "  "));
    }

    [Fact]
    public void FindSame_returns_the_song_that_has_the_title()
    {
        var library = new[] { new SongTitle(1, "Hay Poder"), new SongTitle(2, "Mi Gozo") };

        Assert.Equal(2, SongTitles.FindSame(library, "mi GOZÓ")!.Id);
        Assert.Null(SongTitles.FindSame(library, "Otra"));
    }

    [Fact]
    public void The_same_title_by_another_artist_is_another_song()
    {
        var library = new[]
        {
            new SongTitle(1, "Hay Libertad", "IBI"),
            new SongTitle(2, "Hay Libertad", "Christina Aguilera"),
        };

        Assert.Equal(2, SongTitles.FindSame(library, "hay libertad", "christina AGUILERA")!.Id);
        Assert.Equal(1, SongTitles.FindSame(library, "Hay Libertad", "ibi")!.Id);
        Assert.Null(SongTitles.FindSame(library, "Hay Libertad", "Otro Grupo"));
    }

    [Fact]
    public void A_missing_artist_cannot_tell_songs_apart_so_it_counts_as_the_same()
    {
        var library = new[] { new SongTitle(1, "Hay Libertad", "IBI") };

        Assert.Equal(1, SongTitles.FindSame(library, "Hay Libertad", "")!.Id);
        Assert.Equal(1, SongTitles.FindSame(library, "Hay Libertad")!.Id);
        Assert.Equal(1, SongTitles.FindSame([new SongTitle(1, "Hay Libertad")], "Hay Libertad", "IBI")!.Id);
    }

    [Fact]
    public void The_free_title_takes_the_first_number_that_is_not_used()
    {
        Assert.Equal("Mi Gozo (1)", SongTitles.FreeTitle(["Mi Gozo"], "Mi Gozo"));
        Assert.Equal("Mi Gozo (2)", SongTitles.FreeTitle(["Mi Gozo", "Mi Gozo (1)"], "Mi Gozo"));
    }

    [Fact]
    public void The_free_title_fills_a_hole_in_the_numbers()
    {
        Assert.Equal("Mi Gozo (1)", SongTitles.FreeTitle(["Mi Gozo", "Mi Gozo (2)"], "Mi Gozo"));
    }

    [Fact]
    public void A_title_with_a_number_of_its_own_counts_from_its_base()
    {
        Assert.Equal("Mi Gozo (2)", SongTitles.FreeTitle(["Mi Gozo", "Mi Gozo (1)"], "Mi Gozo (1)"));
    }

    [Fact]
    public void The_free_title_ignores_case_and_accents_of_what_is_taken()
    {
        Assert.Equal("Mi Gozo (2)", SongTitles.FreeTitle(["MI GOZÓ", "mi gozo (1)"], "Mi Gozo"));
    }
}
