using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class SearchWithoutAccentsTests
{
    [Theory]
    [InlineData("SENOR TU ESTAS AQUI")]
    [InlineData("señor, tú estás")]
    [InlineData("cancion del corazon")]
    public void A_song_is_found_without_minding_capitals_accents_or_commas(string typed)
    {
        using var temp = new TempDatabase();
        var songs = new SongRepository(temp.Path);
        songs.Save(new Song
        {
            Title = "Canción del Corazón",
            Sections = [new SongSection { Order = 0, Label = "Coro", Text = "Señor, tú estás aquí" }],
        });

        Assert.Single(songs.Search(typed));
    }

    [Theory]
    [InlineData("JEHOVA TU CARGA")]
    [InlineData("el te sustentara")]
    [InlineData("Él te sustentará")]
    public void A_verse_is_found_without_minding_capitals_or_accents(string typed)
    {
        using var temp = new TempDatabase();
        var bibles = new BibleRepository(temp.Path);
        var version = bibles.Import("Reina Valera", "RVR", "es", new ParsedBible([
            new ParsedBook(19, "Salmos", [new ParsedVerse(55, 22, "Echa sobre Jehová tu carga, y él te sustentará")]),
        ]));

        Assert.Single(bibles.SearchText(version.Id, typed));
    }
}
