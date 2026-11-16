using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Themes;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class ThemeRepository(string dbPath) : IThemeRepository
{
    public IReadOnlyList<SlideTheme> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        return db.Themes.AsNoTracking()
            .Where(t => t.DeletedAt == null && t.OwnerSongId == null)
            .OrderBy(t => t.Name)
            .ToList();
    }

    public SlideTheme? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        return db.Themes.AsNoTracking().FirstOrDefault(t => t.Id == id && t.DeletedAt == null);
    }

    public SlideTheme Save(SlideTheme theme)
    {
        using var db = new AppDbContext(dbPath);
        if (theme.Id == 0)
            db.Themes.Add(theme);
        else
            db.Themes.Update(theme);
        db.SaveChanges();
        return theme;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var theme = db.Themes.Find(id);
        if (theme is null || theme.DeletedAt is not null)
            return;

        // Songs keep pointing at it: while it is in the trash they use the
        // default theme (Get does not return it), and a restore gives it back.
        theme.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    public IReadOnlyList<SlideTheme> GetDeleted()
    {
        using var db = new AppDbContext(dbPath);
        return db.Themes.AsNoTracking()
            .Where(t => t.DeletedAt != null)
            .OrderByDescending(t => t.DeletedAt)
            .ToList();
    }

    public void Restore(int id)
    {
        using var db = new AppDbContext(dbPath);
        var theme = db.Themes.Find(id);
        if (theme is null)
            return;
        theme.DeletedAt = null;
        db.SaveChanges();
    }

    public void Purge(int id)
    {
        using var db = new AppDbContext(dbPath);
        var theme = db.Themes.Find(id);
        if (theme is null || theme.DeletedAt is null)
            return;

        db.Themes.Remove(theme);

        // Erased for good: songs that pointed to it fall back to the default.
        foreach (var song in db.Songs.Where(s => s.ThemeId == id))
            song.ThemeId = null;

        db.SaveChanges();
    }

    public void PurgeOlderThan(DateTime limit)
    {
        var ids = new List<int>();
        using (var db = new AppDbContext(dbPath))
            ids = db.Themes.Where(t => t.DeletedAt != null && t.DeletedAt < limit).Select(t => t.Id).ToList();
        foreach (var id in ids)
            Purge(id);
    }
}
