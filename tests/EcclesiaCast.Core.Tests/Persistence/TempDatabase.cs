using EcclesiaCast.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Core.Tests.Persistence;

/// <summary>A throwaway SQLite file with the real schema, deleted when disposed.</summary>
internal sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ecclesiacast-test-{Guid.NewGuid():N}.db");
        using var db = new AppDbContext(Path);
        db.Database.Migrate();
    }

    public string Path { get; }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in new[] { Path, Path + "-wal", Path + "-shm" })
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
