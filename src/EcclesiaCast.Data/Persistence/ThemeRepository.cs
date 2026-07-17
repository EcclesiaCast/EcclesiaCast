using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Themes;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class ThemeRepository(string dbPath) : IThemeRepository
{
    public IReadOnlyList<SlideTheme> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        return db.Themes.AsNoTracking().OrderBy(t => t.Name).ToList();
    }

    public SlideTheme? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        return db.Themes.AsNoTracking().FirstOrDefault(t => t.Id == id);
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
        if (theme is null)
            return;

        db.Themes.Remove(theme);

        // Songs pointing to the deleted theme fall back to the default.
        foreach (var song in db.Songs.Where(s => s.ThemeId == id))
            song.ThemeId = null;

        db.SaveChanges();
    }
}
