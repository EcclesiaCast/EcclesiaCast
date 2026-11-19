using EcclesiaCast.Core.Songs;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class SongBackgroundTests
{
    private static Song NewSong(string title) => new()
    {
        Title = title,
        Sections = [new SongSection { Order = 0, Label = "Verso", Text = "Letra de " + title }],
    };

    [Fact]
    public void A_song_keeps_its_own_background_and_can_lose_it()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var song = repo.Save(NewSong("Santo"));

        repo.SetBackground(song.Id, 42);
        Assert.Equal(42, repo.Get(song.Id)!.BackgroundMediaId);

        repo.SetBackground(song.Id, null);
        Assert.Null(repo.Get(song.Id)!.BackgroundMediaId);
    }

    [Fact]
    public void Editing_the_lyrics_does_not_take_the_background_away()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var song = repo.Save(NewSong("Santo"));
        repo.SetBackground(song.Id, 7);

        // The copy being edited was loaded before the background was set.
        song.Sections[0].Text = "Otra letra";
        repo.Save(song);

        Assert.Equal(7, repo.Get(song.Id)!.BackgroundMediaId);
    }
}
