using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Tests.Songs;

public class SongHistoryTests
{
    private static Song Song(params string[] texts) => new()
    {
        Id = 7,
        Title = "Ardiendo en fuego",
        Artist = "Latinoamerica",
        Sections = texts.Select((t, i) => new SongSection { Id = 100 + i, Order = i, Label = $"V{i + 1}", Text = t }).ToList(),
    };

    private static string Texts(Song song) => string.Join("|", song.Sections.OrderBy(s => s.Order).Select(s => s.Text));

    [Fact]
    public void Undo_puts_back_the_slides_as_they_were_and_redo_brings_the_change_again()
    {
        var song = Song("uno", "dos", "tres");
        var history = new SongHistory();

        var before = SongSnapshot.Of(song);
        (song.Sections[0].Order, song.Sections[2].Order) = (2, 0); // «tres» goes first
        history.Record(song.Id, before, "mover «V3»");
        Assert.Equal("tres|dos|uno", Texts(song));

        var undone = history.Undo(song.Id, SongSnapshot.Of(song));
        undone!.Value.State.ApplyTo(song);
        Assert.Equal("uno|dos|tres", Texts(song));
        Assert.Equal("mover «V3»", undone.Value.What);

        var redone = history.Redo(song.Id, SongSnapshot.Of(song));
        redone!.Value.State.ApplyTo(song);
        Assert.Equal("tres|dos|uno", Texts(song));
    }

    [Fact]
    public void A_new_change_forgets_what_had_been_undone()
    {
        var song = Song("uno");
        var history = new SongHistory();
        history.Record(song.Id, SongSnapshot.Of(song), "a");
        history.Undo(song.Id, SongSnapshot.Of(song));
        Assert.True(history.CanRedo(song.Id));

        history.Record(song.Id, SongSnapshot.Of(song), "b");

        Assert.False(history.CanRedo(song.Id));
        Assert.Equal("b", history.NextUndo(song.Id));
    }

    [Fact]
    public void Each_song_has_its_own_steps()
    {
        var history = new SongHistory();
        history.Record(1, SongSnapshot.Of(Song("x")), "en la 1");

        Assert.True(history.CanUndo(1));
        Assert.False(history.CanUndo(2));
        Assert.Null(history.Undo(2, SongSnapshot.Of(Song("y"))));
    }

    [Fact]
    public void Only_the_last_steps_are_kept()
    {
        var history = new SongHistory();
        for (var i = 0; i < SongHistory.MaxSteps + 10; i++)
            history.Record(1, SongSnapshot.Of(Song($"paso {i}")), $"paso {i}");

        var count = 0;
        while (history.Undo(1, SongSnapshot.Of(Song("ahora"))) is not null)
            count++;
        Assert.Equal(SongHistory.MaxSteps, count);
    }

    [Fact]
    public void The_design_and_the_title_come_back_too()
    {
        var song = Song("uno");
        song.Sections[0].StyleJson = "{\"FontSize\":80}";
        var before = SongSnapshot.Of(song);

        song.Title = "Otro título";
        song.Sections[0].StyleJson = null;
        before.ApplyTo(song);

        Assert.Equal("Ardiendo en fuego", song.Title);
        Assert.Equal("{\"FontSize\":80}", song.Sections[0].StyleJson);
        Assert.True(before.SameAs(SongSnapshot.Of(song)));
    }

    [Fact]
    public void A_song_never_saved_keeps_no_steps()
    {
        var history = new SongHistory();
        history.Record(0, SongSnapshot.Of(Song("x")), "nueva");

        Assert.False(history.CanUndo(0));
    }
}
