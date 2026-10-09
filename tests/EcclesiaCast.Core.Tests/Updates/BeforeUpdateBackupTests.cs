using EcclesiaCast.Core.Tests.Persistence;
using EcclesiaCast.Data.Persistence;

namespace EcclesiaCast.Core.Tests.Updates;

/// <summary>The copy of the library made before an update. Always on a throwaway database, never the real one.</summary>
public class BeforeUpdateBackupTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ecclesiacast-backup-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        _database.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void The_copy_is_a_library_named_after_the_version_that_is_coming()
    {
        var path = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");

        Assert.Equal(Path.Combine(_folder, "antes-de-1.0.0.db"), path);
        Assert.NotNull(DatabaseBackup.Inspect(path));
    }

    [Fact]
    public void The_copy_is_a_single_file_with_no_journal_beside_it()
    {
        DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");

        Assert.Equal(["antes-de-1.0.0.db"], Directory.GetFiles(_folder).Select(Path.GetFileName));
    }

    [Fact]
    public void The_folder_is_created_when_it_is_missing()
    {
        Assert.False(Directory.Exists(_folder));

        DatabaseBackup.CreateBeforeUpdate(_database.Path, Path.Combine(_folder, "respaldos"), "1.0.0");

        Assert.True(File.Exists(Path.Combine(_folder, "respaldos", "antes-de-1.0.0.db")));
    }

    [Fact]
    public void Only_the_last_three_copies_are_kept()
    {
        var versions = new[] { "1.0.0-beta.8", "1.0.0-beta.9", "1.0.0-beta.10", "1.0.0-rc.1", "1.0.0" };
        var when = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        foreach (var version in versions)
        {
            var path = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, version);
            File.SetLastWriteTimeUtc(path, when);
            when = when.AddDays(1);
        }

        var kept = Directory.GetFiles(_folder).Select(Path.GetFileName).Order().ToList();
        Assert.Equal(["antes-de-1.0.0-beta.10.db", "antes-de-1.0.0-rc.1.db", "antes-de-1.0.0.db"], kept);
    }

    [Fact]
    public void The_newest_copy_is_kept_even_if_the_clock_went_backwards()
    {
        var old = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0-beta.8");
        File.SetLastWriteTimeUtc(old, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        foreach (var version in new[] { "1.0.0-beta.9", "1.0.0-beta.10" })
        {
            var path = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, version);
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        var latest = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");

        Assert.True(File.Exists(latest));
        Assert.Equal(3, Directory.GetFiles(_folder).Length);
    }

    [Fact]
    public void Trying_again_for_the_same_version_replaces_its_copy()
    {
        DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");
        DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");

        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Other_files_in_the_folder_are_never_touched()
    {
        Directory.CreateDirectory(_folder);
        var mine = Path.Combine(_folder, "mi-copia.ecbackup");
        File.WriteAllText(mine, "importante");

        foreach (var version in new[] { "1", "2", "3", "4", "5" })
            DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0." + version);

        Assert.True(File.Exists(mine));
        Assert.Equal(3, Directory.GetFiles(_folder, "antes-de-*.db").Length);
    }

    [Fact]
    public void A_version_with_odd_characters_still_makes_a_valid_file_name()
    {
        var path = DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0/0:x");

        Assert.True(File.Exists(path));
        Assert.Equal(_folder, Path.GetDirectoryName(path));
    }

    [Fact]
    public void The_original_library_is_left_as_it_was()
    {
        var before = new FileInfo(_database.Path).Length;

        DatabaseBackup.CreateBeforeUpdate(_database.Path, _folder, "1.0.0");

        Assert.Equal(before, new FileInfo(_database.Path).Length);
        Assert.NotNull(DatabaseBackup.Inspect(_database.Path));
    }
}
