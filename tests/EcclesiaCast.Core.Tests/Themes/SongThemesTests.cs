using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EcclesiaCast.Core.Tests.Themes;

public class SongThemesTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly ThemeRepository _themes;
    private readonly SongRepository _songs;
    private readonly SongThemes _songThemes;
    private readonly SlideTheme _main;

    public SongThemesTests()
    {
        _themes = new ThemeRepository(_database.Path);
        _songs = new SongRepository(_database.Path);
        _songThemes = new SongThemes(_themes, _songs);
        _main = _themes.Save(new SlideTheme { Name = "Canciones", Kind = ThemeKind.Song, FontFamily = "Arial" });
    }

    public void Dispose() => _database.Dispose();

    private Song NewSong(string title, int? themeId = null, string? styleJson = null) =>
        _songs.Save(new Song
        {
            Title = title,
            ThemeId = themeId,
            Sections = [new SongSection { Order = 0, Label = "Verso 1", Text = "Cuán grande es Él", StyleJson = styleJson }],
        });

    private static SlideTheme Edited(SlideTheme from, string font)
    {
        var look = from.Clone();
        look.FontFamily = font;
        return look;
    }

    private Song Reloaded(Song song) => _songs.Get(song.Id)!;

    [Fact]
    public void A_song_follows_the_main_theme_until_its_theme_is_edited()
    {
        var song = NewSong("Cuán grande es Él");
        Assert.Equal(SongThemeState.FollowsMain, _songThemes.StateOf(song));

        // Opened and saved without changes: nothing happens.
        _songThemes.Save(song, startedFrom: null, editedLook: null);
        Assert.Null(Reloaded(song).ThemeId);
    }

    [Fact]
    public void Editing_the_theme_from_the_song_gives_it_an_own_theme_the_theme_list_does_not_show()
    {
        var song = NewSong("Cuán grande es Él");

        _songThemes.Save(song, startedFrom: null, Edited(_main, "Georgia"));

        song = Reloaded(song);
        Assert.Equal(SongThemeState.Own, _songThemes.StateOf(song));
        var own = _themes.Get(song.ThemeId!.Value)!;
        Assert.Equal(song.Id, own.OwnerSongId);
        Assert.Equal("Georgia", own.FontFamily);
        Assert.Equal(ThemeKind.Song, own.Kind);
        Assert.DoesNotContain(_themes.GetAll(), t => t.Id == own.Id);
    }

    [Fact]
    public void Editing_the_main_theme_does_not_change_a_song_with_its_own_theme()
    {
        var own = NewSong("Propia");
        var plain = NewSong("Sigue al principal");
        _songThemes.Save(own, null, Edited(_main, "Georgia"));

        _main.FontFamily = "Verdana";
        _themes.Save(_main);

        Assert.Equal("Georgia", _themes.Get(Reloaded(own).ThemeId!.Value)!.FontFamily);
        Assert.Null(Reloaded(plain).ThemeId);
    }

    [Fact]
    public void Editing_an_own_theme_again_updates_it_instead_of_making_another()
    {
        var song = NewSong("Cuán grande es Él");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        var first = Reloaded(song).ThemeId!.Value;

        song = Reloaded(song);
        _songThemes.Save(song, startedFrom: first, Edited(_themes.Get(first)!, "Tahoma"));

        Assert.Equal(first, Reloaded(song).ThemeId);
        Assert.Equal("Tahoma", _themes.Get(first)!.FontFamily);
    }

    [Fact]
    public void An_own_theme_saved_without_changes_stays()
    {
        var song = NewSong("Cuán grande es Él");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var own = song.ThemeId!.Value;

        _songThemes.Save(song, startedFrom: own, editedLook: null);

        Assert.Equal(own, Reloaded(song).ThemeId);
        Assert.NotNull(_themes.Get(own));
    }

    [Fact]
    public void Going_back_to_the_main_theme_deletes_the_own_theme()
    {
        var song = NewSong("Cuán grande es Él");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var own = song.ThemeId!.Value;

        _songThemes.FollowMain(song);

        Assert.Null(Reloaded(song).ThemeId);
        Assert.Null(_themes.Get(own));
        Assert.Equal(SongThemeState.FollowsMain, _songThemes.StateOf(Reloaded(song)));
    }

    [Fact]
    public void Picking_another_theme_without_changes_links_the_song_to_it()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        var song = NewSong("Noche de paz");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var own = song.ThemeId!.Value;

        _songThemes.Save(song, startedFrom: other.Id, editedLook: null);

        Assert.Equal(other.Id, Reloaded(song).ThemeId);
        Assert.Equal(SongThemeState.Chosen, _songThemes.StateOf(Reloaded(song)));
        Assert.Null(_themes.Get(own));
    }

    [Fact]
    public void Starting_from_another_theme_and_changing_it_leaves_that_theme_alone()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song, FontFamily = "Arial" });
        var song = NewSong("Noche de paz");

        _songThemes.Save(song, startedFrom: other.Id, Edited(other, "Georgia"));

        Assert.Equal(SongThemeState.Own, _songThemes.StateOf(Reloaded(song)));
        Assert.Equal("Arial", _themes.Get(other.Id)!.FontFamily);
    }

    [Fact]
    public void Choosing_another_theme_in_the_lyrics_editor_drops_the_own_one()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        var song = NewSong("Noche de paz");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var own = song.ThemeId!.Value;

        song.ThemeId = other.Id;
        _songs.Save(song);

        Assert.Null(_themes.Get(own));
    }

    [Fact]
    public void Saving_the_lyrics_keeps_the_own_theme()
    {
        var song = NewSong("Noche de paz");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var own = song.ThemeId!.Value;

        song.Sections[0].Text = "Noche de paz, noche de amor";
        _songs.Save(song);

        Assert.Equal(own, Reloaded(song).ThemeId);
        Assert.NotNull(_themes.Get(own));
    }

    [Fact]
    public void Deleting_a_song_deletes_its_own_theme()
    {
        var song = NewSong("Noche de paz");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        var own = Reloaded(song).ThemeId!.Value;

        _songs.Delete(song.Id);

        Assert.Null(_themes.Get(own));
        Assert.NotNull(_themes.Get(_main.Id));
    }

    [Fact]
    public void A_copied_song_gets_its_own_copy_of_the_own_theme()
    {
        var song = NewSong("Noche de paz");
        _songThemes.Save(song, null, Edited(_main, "Georgia"));
        song = Reloaded(song);
        var copy = NewSong("Noche de paz (copia)");

        _songThemes.CopyTheme(song, copy);

        copy = Reloaded(copy);
        Assert.Equal(SongThemeState.Own, _songThemes.StateOf(copy));
        Assert.NotEqual(song.ThemeId, copy.ThemeId);
        Assert.Equal("Georgia", _themes.Get(copy.ThemeId!.Value)!.FontFamily);

        // Each one changes apart.
        _songThemes.FollowMain(copy);
        Assert.NotNull(_themes.Get(song.ThemeId!.Value));
    }

    [Fact]
    public void A_copied_song_shares_a_chosen_theme()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        var song = NewSong("Noche de paz", other.Id);
        var copy = NewSong("Noche de paz (copia)");

        _songThemes.CopyTheme(song, copy);

        Assert.Equal(other.Id, Reloaded(copy).ThemeId);
    }

    [Fact]
    public void The_survey_counts_what_applying_to_all_would_change()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        NewSong("Sigue al principal");
        NewSong("Con otro tema", other.Id);
        NewSong("Diseñada", styleJson: "{\"FontSize\":80}");
        _songThemes.Save(NewSong("Propia"), null, Edited(_main, "Georgia"));

        var survey = _songThemes.Survey();

        Assert.Equal(1, survey.Own);
        Assert.Equal(1, survey.Chosen);
        Assert.Equal(1, survey.Designed);
        Assert.Equal(2, survey.Apart);
    }

    [Fact]
    public void Applying_to_all_puts_every_song_back_on_the_main_theme_and_keeps_slide_designs()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        var chosen = NewSong("Con otro tema", other.Id);
        var designed = NewSong("Diseñada", styleJson: "{\"FontSize\":80}");
        var own = NewSong("Propia");
        _songThemes.Save(own, null, Edited(_main, "Georgia"));
        var ownTheme = Reloaded(own).ThemeId!.Value;

        var changed = _songThemes.ApplyMainToAll(clearDesigns: false);

        Assert.Equal(2, changed);
        Assert.All(_songs.Search(string.Empty), s => Assert.Null(s.ThemeId));
        Assert.Null(_themes.Get(ownTheme));
        Assert.NotNull(_themes.Get(other.Id));
        Assert.Equal("{\"FontSize\":80}", Reloaded(designed).Sections[0].StyleJson);
        Assert.Equal(SongThemeState.FollowsMain, _songThemes.StateOf(Reloaded(chosen)));
    }

    [Fact]
    public void Applying_to_all_can_clear_the_slide_designs_too()
    {
        var designed = NewSong("Diseñada", styleJson: "{\"FontSize\":80}");
        var own = NewSong("Propia", styleJson: "{\"Bold\":false}");
        _songThemes.Save(own, null, Edited(_main, "Georgia"));

        var changed = _songThemes.ApplyMainToAll(clearDesigns: true);

        Assert.Equal(2, changed);
        Assert.Null(Reloaded(designed).Sections[0].StyleJson);
        Assert.Null(Reloaded(own).Sections[0].StyleJson);
        Assert.Null(Reloaded(own).ThemeId);
        Assert.Equal("Cuán grande es Él", Reloaded(own).Sections[0].Text);
    }

    [Fact]
    public void The_look_ignores_name_id_kind_and_owner()
    {
        var a = new SlideTheme { Id = 3, Name = "Uno", Kind = ThemeKind.Song };
        var b = a.Clone();
        b.Id = 9;
        b.Name = "Otro";
        b.Kind = ThemeKind.Bible;
        b.OwnerSongId = 4;

        Assert.True(SongThemes.SameLook(a, b));
    }

    [Fact]
    public void The_look_sees_a_change_but_not_a_rounding()
    {
        var a = new SlideTheme { SecondaryScale = 0.62, LineBandOpacity = 0.7 };

        var rounded = a.Clone();
        rounded.SecondaryScale = 0.62 * 100 / 100;
        rounded.LineBandOpacity = 70.0 / 100;
        Assert.True(SongThemes.SameLook(a, rounded));

        var coloured = a.Clone();
        coloured.TextColor = "#FFD700";
        Assert.False(SongThemes.SameLook(a, coloured));

        var boxed = a.Clone();
        boxed.BoxX = 110;
        Assert.False(SongThemes.SameLook(a, boxed));
    }

    [Fact]
    public void Songs_saved_before_own_themes_existed_look_the_same_after_the_upgrade()
    {
        var other = _themes.Save(new SlideTheme { Name = "Navidad", Kind = ThemeKind.Song });
        var plain = NewSong("Sigue al principal");
        var chosen = NewSong("Con otro tema", other.Id);

        // Back to the database as the 1.3.1 had it, and up again.
        using (var db = new AppDbContext(_database.Path))
            db.GetService<IMigrator>().Migrate("20261108044036_AddLineBands");
        using (var db = new AppDbContext(_database.Path))
            db.Database.Migrate();

        Assert.Equal(SongThemeState.FollowsMain, _songThemes.StateOf(Reloaded(plain)));
        Assert.Equal(SongThemeState.Chosen, _songThemes.StateOf(Reloaded(chosen)));
        Assert.Equal(other.Id, Reloaded(chosen).ThemeId);
        Assert.Contains(_themes.GetAll(), t => t.Id == other.Id);
        Assert.Contains(_themes.GetAll(), t => t.Id == _main.Id);
    }
}
