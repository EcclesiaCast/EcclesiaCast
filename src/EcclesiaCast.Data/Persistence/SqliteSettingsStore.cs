using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Data.Persistence;

/// <summary>Settings persisted in the local SQLite database.</summary>
/// <remarks>The database schema is migrated once at app startup.</remarks>
public sealed class SqliteSettingsStore(string dbPath) : ISettingsStore
{
    public string? Get(string key)
    {
        using var db = new AppDbContext(dbPath);
        return db.Settings.Find(key)?.Value;
    }

    public void Set(string key, string value)
    {
        using var db = new AppDbContext(dbPath);
        var existing = db.Settings.Find(key);
        if (existing is null)
            db.Settings.Add(new Setting { Key = key, Value = value });
        else
            existing.Value = value;
        db.SaveChanges();
    }
}
