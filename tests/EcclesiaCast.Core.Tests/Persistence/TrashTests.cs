using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Persistence;

public class TrashTests
{
    private static Song NewSong(string title) => new()
    {
        Title = title,
        Sections = [new SongSection { Order = 0, Label = "Verso", Text = "Letra de " + title }],
    };

    [Fact]
    public void Borrar_una_cancion_la_manda_a_la_papelera_y_no_aparece_en_la_biblioteca()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var song = repo.Save(NewSong("Cuan grande es Él"));
        repo.Save(NewSong("Santo"));

        repo.Delete(song.Id);

        Assert.Null(repo.Get(song.Id));
        Assert.DoesNotContain(repo.Search(), s => s.Id == song.Id);
        Assert.DoesNotContain(repo.GetTitles(), s => s.Id == song.Id);
        var trashed = Assert.Single(repo.GetDeleted());
        Assert.Equal(song.Id, trashed.Id);
        Assert.NotNull(trashed.DeletedAt);
    }

    [Fact]
    public void Restaurar_devuelve_la_cancion_con_su_letra()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var song = repo.Save(NewSong("Santo"));
        repo.Delete(song.Id);

        repo.Restore(song.Id);

        var back = repo.Get(song.Id);
        Assert.NotNull(back);
        Assert.Single(back!.Sections);
        Assert.Empty(repo.GetDeleted());
    }

    [Fact]
    public void Purge_borra_del_todo_pero_solo_lo_que_esta_en_la_papelera()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var kept = repo.Save(NewSong("Queda"));
        var gone = repo.Save(NewSong("Se va"));
        repo.Delete(gone.Id);

        repo.Purge(kept.Id); // not trashed: must survive
        repo.Purge(gone.Id);

        Assert.NotNull(repo.Get(kept.Id));
        Assert.Empty(repo.GetDeleted());
    }

    [Fact]
    public void PurgeOlderThan_borra_lo_viejo_y_deja_lo_reciente()
    {
        using var db = new TempDatabase();
        var repo = new SongRepository(db.Path);
        var old = repo.Save(NewSong("Vieja"));
        var recent = repo.Save(NewSong("Reciente"));
        repo.Delete(old.Id);
        repo.Delete(recent.Id);

        // Everything trashed "now" is older than tomorrow, nothing is older than yesterday.
        repo.PurgeOlderThan(DateTime.UtcNow.AddDays(-1));
        Assert.Equal(2, repo.GetDeleted().Count);

        repo.PurgeOlderThan(DateTime.UtcNow.AddDays(1));
        Assert.Empty(repo.GetDeleted());
    }

    [Fact]
    public void Un_tema_en_la_papelera_no_se_ofrece_y_las_canciones_siguen_apuntandole()
    {
        using var db = new TempDatabase();
        var themes = new ThemeRepository(db.Path);
        var songs = new SongRepository(db.Path);
        var theme = themes.Save(new SlideTheme { Name = "Azul", Kind = ThemeKind.Song });
        var song = NewSong("Con tema");
        song.ThemeId = theme.Id;
        song = songs.Save(song);

        themes.Delete(theme.Id);

        Assert.Null(themes.Get(theme.Id));
        Assert.DoesNotContain(themes.GetAll(), t => t.Id == theme.Id);
        Assert.Single(themes.GetDeleted());
        Assert.Equal(theme.Id, songs.Get(song.Id)!.ThemeId);

        themes.Restore(theme.Id);
        Assert.NotNull(themes.Get(theme.Id));
    }

    [Fact]
    public void Purgar_un_tema_suelta_las_canciones_que_lo_usaban()
    {
        using var db = new TempDatabase();
        var themes = new ThemeRepository(db.Path);
        var songs = new SongRepository(db.Path);
        var theme = themes.Save(new SlideTheme { Name = "Azul", Kind = ThemeKind.Song });
        var song = NewSong("Con tema");
        song.ThemeId = theme.Id;
        song = songs.Save(song);
        themes.Delete(theme.Id);

        themes.Purge(theme.Id);

        Assert.Empty(themes.GetDeleted());
        Assert.Null(songs.Get(song.Id)!.ThemeId);
    }
}
