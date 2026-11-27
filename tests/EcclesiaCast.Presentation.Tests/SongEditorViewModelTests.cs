using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class SongEditorViewModelTests
{
    private static readonly IReadOnlyList<SlideTheme> Themes =
        [new SlideTheme { Id = 1, Name = "Azul" }, new SlideTheme { Id = 2, Name = "Negro" }];

    private static Song Existing(int? theme = null) => new()
    {
        Id = 7,
        Title = "Una",
        Artist = "Alguien",
        ThemeId = theme,
        Sections =
        [
            new SongSection { Label = "Verso", Text = "uno", StyleJson = "{\"a\":1}" },
            new SongSection { Label = "Verso", Text = "dos" },
        ],
    };

    [Fact]
    public void A_new_song_starts_empty_with_the_main_theme()
    {
        var editor = new SongEditorViewModel(null, Themes);

        Assert.Equal("Nueva canción", editor.WindowTitle);
        Assert.Equal(string.Empty, editor.InitialLyrics);
        Assert.Equal(["(Tema principal)", "Azul", "Negro"], editor.Themes.Select(t => t.Label));
        Assert.Null(editor.SelectedTheme!.Id);
    }

    [Fact]
    public void A_shared_theme_of_the_song_comes_chosen()
    {
        var editor = new SongEditorViewModel(Existing(theme: 2), Themes);

        Assert.Equal("Editar canción", editor.WindowTitle);
        Assert.Equal("Negro", editor.SelectedTheme!.Label);
        Assert.Equal(3, editor.Themes.Count);
    }

    [Fact]
    public void A_theme_of_its_own_is_offered_apart_and_survives_saving()
    {
        var editor = new SongEditorViewModel(Existing(theme: 40), Themes);

        Assert.Equal("(Tema propio de esta canción)", editor.Themes[1].Label);
        Assert.Equal(40, editor.SelectedTheme!.Id);
        Assert.Null(editor.TrySave("Una", "", "", "uno"));
        Assert.Equal(40, editor.Result!.ThemeId);
    }

    [Theory]
    [InlineData("  ", "letra", SongEditorField.Title, "La canción necesita un título.")]
    [InlineData("Título", " \n \n", SongEditorField.Lyrics, "La letra está vacía.")]
    public void What_is_missing_is_said_and_pointed_at(string title, string lyrics, SongEditorField field, string error)
    {
        var editor = new SongEditorViewModel(null, Themes);

        Assert.Equal(field, editor.TrySave(title, "", "", lyrics));
        Assert.Equal(error, editor.Error);
        Assert.Null(editor.Result);
    }

    [Fact]
    public void The_song_is_made_trimmed_and_keeps_the_designs_by_position()
    {
        var editor = new SongEditorViewModel(Existing(), Themes);
        editor.SelectedTheme = editor.Themes[1];

        Assert.Null(editor.TrySave("  Otra  ", " Autor ", "   ", "tres\n\ncuatro\n\ncinco"));

        var song = editor.Result!;
        Assert.Equal(7, song.Id);
        Assert.Equal("Otra", song.Title);
        Assert.Equal("Autor", song.Artist);
        Assert.Null(song.Copyright);
        Assert.Equal(1, song.ThemeId);
        Assert.Equal(["{\"a\":1}", null, null], song.Sections.Select(s => s.StyleJson));
    }
}
