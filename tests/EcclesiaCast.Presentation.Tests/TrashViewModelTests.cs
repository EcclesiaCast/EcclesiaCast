using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

[Collection("ViewModels")]
public sealed class TrashViewModelTests
{
    private static Song NewSong(string title) => new()
    {
        Title = title,
        Sections = [new SongSection { Order = 0, Label = "Verso", Text = "Letra" }],
    };

    private static (ViewModelWorld World, Song Song, SlideTheme Theme) WorldWithTrash()
    {
        var world = new ViewModelWorld();
        var song = world.Songs.Save(NewSong("Cuán grande es Él"));
        var theme = world.Themes.Save(new SlideTheme { Name = "Tema azul" });
        world.Songs.Delete(song.Id);
        world.Themes.Delete(theme.Id);
        return (world, song, theme);
    }

    [Fact]
    public void Lists_the_deleted_songs_and_themes_with_their_time_left()
    {
        var (world, song, theme) = WorldWithTrash();
        using var _ = world;

        var vm = new TrashViewModel(world.Songs, world.Themes);

        Assert.False(vm.IsEmpty);
        Assert.Equal(2, vm.Entries.Count);
        var songEntry = Assert.Single(vm.Entries, e => e.IsSong);
        Assert.Equal(song.Id, songEntry.Id);
        Assert.Equal("Canción: Cuán grande es Él", songEntry.Title);
        Assert.Contains("Borrada el ", songEntry.Detail);
        Assert.EndsWith("quedan 30 días", songEntry.Detail);
        var themeEntry = Assert.Single(vm.Entries, e => !e.IsSong);
        Assert.Equal(theme.Id, themeEntry.Id);
        Assert.Equal("Tema: Tema azul", themeEntry.Title);
    }

    [Fact]
    public void The_time_left_reads_like_it_always_did()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(string.Empty, TrashViewModel.Describe(null, now));
        Assert.EndsWith("se borra del todo hoy", TrashViewModel.Describe(now.AddDays(-31), now));
        Assert.EndsWith("queda 1 día", TrashViewModel.Describe(now.AddDays(-29).AddHours(-1), now));
        Assert.EndsWith("quedan 10 días", TrashViewModel.Describe(now.AddDays(-20), now));
    }

    [Fact]
    public void Is_empty_when_nothing_was_deleted()
    {
        using var world = new ViewModelWorld();
        world.Songs.Save(NewSong("Viva"));

        var vm = new TrashViewModel(world.Songs, world.Themes);

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void Restoring_a_song_puts_it_back_in_the_library_and_out_of_the_list()
    {
        var (world, song, _) = WorldWithTrash();
        using var _w = world;
        var vm = new TrashViewModel(world.Songs, world.Themes);

        vm.Restore(vm.Entries.Single(e => e.IsSong));

        Assert.Single(vm.Entries);
        Assert.False(vm.Entries[0].IsSong);
        Assert.Contains(world.Songs.Search(), s => s.Id == song.Id);
        Assert.Empty(world.Songs.GetDeleted());
    }

    [Fact]
    public void Restoring_a_theme_puts_it_back()
    {
        var (world, _, theme) = WorldWithTrash();
        using var _w = world;
        var vm = new TrashViewModel(world.Songs, world.Themes);

        vm.Restore(vm.Entries.Single(e => !e.IsSong));

        Assert.Contains(world.Themes.GetAll(), t => t.Id == theme.Id);
        Assert.Empty(world.Themes.GetDeleted());
    }

    [Fact]
    public void Erasing_for_good_asks_first_and_does_nothing_if_the_answer_is_no()
    {
        var (world, _, _) = WorldWithTrash();
        using var _w = world;
        using var notices = new ScriptedNotices().No();
        var vm = new TrashViewModel(world.Songs, world.Themes);
        var entry = vm.Entries.Single(e => e.IsSong);

        var erased = vm.Purge(entry);

        Assert.False(erased);
        Assert.Equal(2, vm.Entries.Count);
        Assert.Single(world.Songs.GetDeleted());
        var asked = Assert.Single(notices.Asked);
        Assert.Equal("¿Borrar del todo «Canción: Cuán grande es Él»?\n\nEsto no se puede deshacer.", asked.Message);
        Assert.Equal(["Cancelar", "Borrar del todo"], asked.Buttons);
    }

    [Fact]
    public void Erasing_for_good_removes_the_song_when_the_answer_is_yes()
    {
        var (world, _, _) = WorldWithTrash();
        using var _w = world;
        using var notices = new ScriptedNotices().Yes();
        var vm = new TrashViewModel(world.Songs, world.Themes);

        var erased = vm.Purge(vm.Entries.Single(e => e.IsSong));

        Assert.True(erased);
        Assert.Single(vm.Entries);
        Assert.Empty(world.Songs.GetDeleted());
        Assert.Single(world.Themes.GetDeleted());
    }

    [Fact]
    public void Erasing_a_theme_for_good_leaves_the_songs_alone()
    {
        var (world, _, _) = WorldWithTrash();
        using var _w = world;
        using var notices = new ScriptedNotices().Yes();
        var vm = new TrashViewModel(world.Songs, world.Themes);

        vm.Purge(vm.Entries.Single(e => !e.IsSong));

        Assert.Empty(world.Themes.GetDeleted());
        Assert.Single(world.Songs.GetDeleted());
    }

    [Fact]
    public void Emptying_the_trash_asks_with_the_count_and_erases_everything_on_yes()
    {
        var (world, _, _) = WorldWithTrash();
        using var _w = world;
        using var notices = new ScriptedNotices().Yes();
        var vm = new TrashViewModel(world.Songs, world.Themes);

        var erased = vm.EmptyAll();

        Assert.True(erased);
        Assert.True(vm.IsEmpty);
        Assert.Empty(world.Songs.GetDeleted());
        Assert.Empty(world.Themes.GetDeleted());
        Assert.Equal("¿Vaciar la papelera? Se borran del todo 2 elemento(s).\n\nEsto no se puede deshacer.",
            notices.Asked[0].Message);
    }

    [Fact]
    public void Emptying_the_trash_does_nothing_on_no()
    {
        var (world, _, _) = WorldWithTrash();
        using var _w = world;
        using var notices = new ScriptedNotices().No();
        var vm = new TrashViewModel(world.Songs, world.Themes);

        Assert.False(vm.EmptyAll());

        Assert.Equal(2, vm.Entries.Count);
        Assert.Single(world.Songs.GetDeleted());
        Assert.Single(world.Themes.GetDeleted());
    }
}
