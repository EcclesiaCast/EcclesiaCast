using EcclesiaCast.Core.Bible;
using EcclesiaCast.Data.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Core.Tests.Persistence;

/// <summary>
/// A library as the 1.4.0 program (.NET 8, EF Core 8) left it on a church's
/// computer, opened by the code that runs on .NET 10. The file
/// <c>library-1.4.0.db</c> was made by the .NET 8 build with the migrations of
/// that version: one song, "Sublime Gracia", and one verse, John 3:16.
/// </summary>
public class LibraryFromDotNet8Tests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-net10-{Guid.NewGuid():N}");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private string CopyOfTheOldLibrary()
    {
        Directory.CreateDirectory(_folder);
        var copy = Path.Combine(_folder, "ecclesiacast.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Persistence", "library-1.4.0.db"), copy);
        return copy;
    }

    [Fact]
    public void The_old_library_has_nothing_left_to_migrate_and_migrating_it_changes_nothing()
    {
        var path = CopyOfTheOldLibrary();

        using (var db = new AppDbContext(path))
        {
            Assert.Empty(db.Database.GetPendingMigrations());
            db.Database.Migrate();
        }

        using var again = new AppDbContext(path);
        Assert.Equal(again.Database.GetMigrations(), again.Database.GetAppliedMigrations());
    }

    [Fact]
    public void A_song_and_a_verse_saved_by_the_old_program_are_read_by_the_new_one()
    {
        var path = CopyOfTheOldLibrary();
        using (var db = new AppDbContext(path))
            db.Database.Migrate();

        var songs = new SongRepository(path);
        var found = Assert.Single(songs.Search("gracia"));
        var song = songs.Get(found.Id)!;
        Assert.Equal("Sublime Gracia", song.Title);
        Assert.Equal("John Newton", song.Artist);
        Assert.Equal(new DateTime(2026, 11, 1, 12, 0, 0), song.AddedAt);
        var section = Assert.Single(song.Sections);
        Assert.Equal("Verso 1", section.Label);
        Assert.StartsWith("Sublime gracia del Señor", section.Text);

        var bible = new BibleRepository(path);
        var version = Assert.Single(bible.GetVersions());
        var verse = Assert.Single(bible.GetPassage(version.Id, new BibleReference(43, 3, 16, 16)));
        Assert.Equal("Juan", verse.BookName);
        Assert.StartsWith("Porque de tal manera amó Dios", verse.Text);
    }

    [Fact]
    public void The_old_library_still_takes_new_songs_after_opening_it()
    {
        var path = CopyOfTheOldLibrary();
        using (var db = new AppDbContext(path))
            db.Database.Migrate();

        var songs = new SongRepository(path);
        songs.Save(new EcclesiaCast.Core.Songs.Song { Title = "Cuán grande es Él" });

        Assert.Equal(2, songs.Search().Count);
    }

    [Fact]
    public void A_backup_of_the_old_library_is_a_single_file_the_program_can_inspect()
    {
        var path = CopyOfTheOldLibrary();
        using (var db = new AppDbContext(path))
            db.Database.Migrate();

        // The program keeps the library open while it makes the copy.
        using var open = new SqliteConnection($"Data Source={path}");
        open.Open();

        var backup = Path.Combine(_folder, "respaldo.ecbackup");
        DatabaseBackup.Create(path, backup);

        Assert.Equal(["respaldo.ecbackup"],
            Directory.GetFiles(_folder).Select(Path.GetFileName).Where(n => n!.StartsWith("respaldo")));
        var summary = DatabaseBackup.Inspect(backup)!;
        Assert.Equal(1, summary.Songs);
        Assert.Equal(1, summary.BibleVersions);

        using var check = new SqliteConnection($"Data Source={backup};Mode=ReadOnly");
        check.Open();
        using var command = check.CreateCommand();
        command.CommandText = "PRAGMA journal_mode";
        Assert.Equal("delete", command.ExecuteScalar());
    }

    [Fact]
    public void A_backup_of_a_library_that_works_in_wal_mode_is_still_a_single_file()
    {
        var path = CopyOfTheOldLibrary();
        using (var db = new AppDbContext(path))
            db.Database.Migrate();

        // The live library may run with a write-ahead log; the copy must not keep it.
        using var open = new SqliteConnection($"Data Source={path}");
        open.Open();
        using (var wal = open.CreateCommand())
        {
            wal.CommandText = "PRAGMA journal_mode=WAL";
            Assert.Equal("wal", wal.ExecuteScalar());
        }
        new SongRepository(path).Save(new EcclesiaCast.Core.Songs.Song { Title = "Cuán grande es Él" });

        var backup = Path.Combine(_folder, "respaldo.ecbackup");
        DatabaseBackup.Create(path, backup);

        Assert.False(File.Exists(backup + "-wal"));
        Assert.False(File.Exists(backup + "-shm"));
        Assert.Equal(2, DatabaseBackup.Inspect(backup)!.Songs);

        using var check = new SqliteConnection($"Data Source={backup};Mode=ReadOnly");
        check.Open();
        using var command = check.CreateCommand();
        command.CommandText = "PRAGMA journal_mode";
        Assert.Equal("delete", command.ExecuteScalar());
    }
}
