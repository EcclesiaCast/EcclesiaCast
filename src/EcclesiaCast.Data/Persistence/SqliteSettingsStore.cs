using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Data.Persistence;

/// <summary>Settings persisted in the local SQLite database.</summary>
public sealed class SqliteSettingsStore : ISettingsStore
{
    private readonly string _dbPath;

    public SqliteSettingsStore(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var db = new AppDbContext(_dbPath);
        db.Database.EnsureCreated();
    }

    public string? Get(string key)
    {
        using var db = new AppDbContext(_dbPath);
        return db.Settings.Find(key)?.Value;
    }

    public void Set(string key, string value)
    {
        using var db = new AppDbContext(_dbPath);
        var existing = db.Settings.Find(key);
        if (existing is null)
            db.Settings.Add(new Setting { Key = key, Value = value });
        else
            existing.Value = value;
        db.SaveChanges();
    }
}
