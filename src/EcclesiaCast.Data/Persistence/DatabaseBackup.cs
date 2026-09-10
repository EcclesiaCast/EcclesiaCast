using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

/// <summary>What a backup file turned out to hold, for the operator to confirm.</summary>
public sealed record BackupSummary(int Songs, int BibleVersions, int MediaItems, int Playlists);

/// <summary>
/// How a staged restore went at startup. A restore that could not happen has
/// to be said out loud: the operator asked for their library back and would
/// otherwise carry on believing they got it.
/// </summary>
public sealed record RestoreResult(bool WasStaged, bool Restored, string? Error)
{
    public static readonly RestoreResult NothingStaged = new(false, false, null);
}

/// <summary>
/// Saving the church's library to a file and putting it back.
///
/// Everything the program knows lives in one SQLite file, so a backup is a
/// copy of it — but a plain file copy of a database the program has open can
/// land mid-write and come back corrupt. SQLite's own backup API copies a
/// consistent snapshot with the app running, which is the whole point: nobody
/// is going to close EcclesiaCast to make a backup.
///
/// Restoring cannot swap the file while the program is holding it open, so it
/// is staged instead: the chosen file is left beside the database and swapped
/// in on the next start, before anything opens it.
/// </summary>
public static class DatabaseBackup
{
    /// <summary>Suffix of the staged file waiting to be swapped in at startup.</summary>
    private const string PendingSuffix = ".restore-pending";

    /// <summary>Suffix of the library that was replaced, kept as a way back.</summary>
    private const string ReplacedSuffix = ".before-restore";

    public static string SuggestedFileName(DateTimeOffset now) =>
        $"EcclesiaCast {now:yyyy-MM-dd}.ecbackup";

    /// <summary>Writes a consistent copy of the library to <paramref name="destination"/>.</summary>
    public static void Create(string dbPath, string destination)
    {
        // BackupDatabase copies into whatever database is already there, so a
        // leftover file could keep tables of its own. Start from nothing.
        Delete(destination);

        using var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        source.Open();
        using var target = new SqliteConnection($"Data Source={destination}");
        target.Open();
        source.BackupDatabase(target);

        // The copy inherits the live database's write-ahead log, which would
        // leave a "-wal" and a "-shm" beside it. A backup has to be the single
        // file the operator drags onto a pendrive, so fold the log back in.
        using var checkpoint = target.CreateCommand();
        checkpoint.CommandText = "PRAGMA journal_mode=DELETE";
        checkpoint.ExecuteNonQuery();
    }

    /// <summary>
    /// Reads what a candidate file holds, or null when it is not an
    /// EcclesiaCast library at all (someone picked the wrong file).
    /// </summary>
    public static BackupSummary? Inspect(string file)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={file};Mode=ReadOnly");
            connection.Open();

            if (!TableExists(connection, "Songs") || !TableExists(connection, "Themes"))
                return null;

            return new BackupSummary(
                Count(connection, "Songs"),
                Count(connection, "BibleVersions"),
                Count(connection, "MediaItems"),
                Count(connection, "Playlists"));
        }
        catch (SqliteException)
        {
            // Not a database, or not one we can read: same answer either way.
            return null;
        }
    }

    /// <summary>
    /// True when the file was written by a newer EcclesiaCast than this one.
    /// Restoring it would leave the program facing tables it does not know, so
    /// the operator is told to update instead.
    /// </summary>
    public static bool IsFromANewerVersion(string file, string dbPath)
    {
        var applied = AppliedMigrations(file);
        if (applied.Count == 0)
            return false;

        using var db = new AppDbContext(dbPath);
        var known = db.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);

        return !known.Contains(applied[^1]);
    }

    /// <summary>
    /// Leaves <paramref name="file"/> ready to become the library on the next
    /// start. Nothing is replaced yet, so a program that never restarts (a
    /// crash, a power cut) simply keeps working with what it has.
    /// </summary>
    public static void StageRestore(string file, string dbPath)
    {
        var pending = dbPath + PendingSuffix;
        Delete(pending);
        File.Copy(file, pending);
    }

    /// <summary>
    /// Swaps in a staged restore. Called at startup, before anything opens the
    /// database. Returns true when a restore actually happened.
    /// </summary>
    public static RestoreResult ApplyPendingRestore(string dbPath)
    {
        var pending = dbPath + PendingSuffix;
        if (!File.Exists(pending))
            return RestoreResult.NothingStaged;

        var replaced = dbPath + ReplacedSuffix;
        var movedAside = false;

        try
        {
            if (File.Exists(dbPath))
            {
                Delete(replaced);
                File.Move(dbPath, replaced);
                movedAside = true;
            }

            // The journal belongs to the library being replaced. Left behind,
            // SQLite would try to recover it onto the restored file.
            Delete(dbPath + "-wal");
            Delete(dbPath + "-shm");

            File.Move(pending, dbPath);
            return new RestoreResult(true, true, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Something holds one of the files — another copy of the program
            // running, a backup tool, an antivirus. Put the library back so
            // the church is never left without one, and keep the staged copy
            // for the next start.
            if (movedAside && !File.Exists(dbPath))
            {
                try { File.Move(replaced, dbPath); }
                catch (IOException) { /* Se avisa igual; la copia sigue en su lugar. */ }
            }

            return new RestoreResult(true, false, ex.Message);
        }
    }

    /// <summary>Where the library that a restore replaced was left.</summary>
    public static string ReplacedLibraryPath(string dbPath) => dbPath + ReplacedSuffix;

    private static IReadOnlyList<string> AppliedMigrations(string file)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={file};Mode=ReadOnly");
            connection.Open();

            if (!TableExists(connection, "__EFMigrationsHistory"))
                return [];

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
            using var reader = command.ExecuteReader();

            var ids = new List<string>();
            while (reader.Read())
                ids.Add(reader.GetString(0));

            return ids;
        }
        catch (SqliteException)
        {
            return [];
        }
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", table);
        return command.ExecuteScalar() is not null;
    }

    private static int Count(SqliteConnection connection, string table)
    {
        if (!TableExists(connection, table))
            return 0;

        using var command = connection.CreateCommand();
        // The table name comes from this file, never from the user.
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
