using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class SearchAcrossLineBreaksTests
{
    [Fact]
    public void A_phrase_split_by_a_line_break_still_finds_the_song()
    {
        using var temp = new TempDatabase();
        var songs = new SongRepository(temp.Path);
        songs.Save(new Song
        {
            Title = "Hay Libertad",
            Sections = [new SongSection { Order = 0, Label = "Coro", Text = "Rompe las cadenas\nde la muerte\r\ny canta" }],
        });
        songs.Save(new Song
        {
            Title = "Otra",
            Sections = [new SongSection { Order = 0, Label = "Coro", Text = "Nada que ver" }],
        });

        var found = songs.Search("las cadenas de la muerte");

        Assert.Equal(["Hay Libertad"], found.Select(s => s.Title));
    }

    [Fact]
    public void Extra_spaces_in_what_is_typed_do_not_break_the_search()
    {
        using var temp = new TempDatabase();
        var songs = new SongRepository(temp.Path);
        songs.Save(new Song
        {
            Title = "Hay Libertad",
            Sections = [new SongSection { Order = 0, Label = "Coro", Text = "las cadenas\nde la muerte" }],
        });

        Assert.Single(songs.Search("  las   cadenas\tde la  muerte "));
    }

    [Fact]
    public void A_verse_with_a_line_break_matches_a_one_line_query()
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var version = bibles.Import("Reina Valera", "RVR1960", "es", new ParsedBible([
            new ParsedBook(19, "Salmos", [new ParsedVerse(55, 22, "Echa sobre Jehová\ntu carga, y él te sustentará")]),
        ]));

        var found = bibles.SearchText(version.Id, "echa sobre jehová tu carga");

        Assert.Equal("Salmos 55:22", Assert.Single(found).Reference);
    }
}
