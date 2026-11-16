using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Search;
using EcclesiaCast.Core.Songs;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class SongRepository(string dbPath) : ISongRepository
{
    public IReadOnlyList<Song> Search(string? query = null)
    {
        using var db = new AppDbContext(dbPath);
        IQueryable<Song> songs = db.Songs.Include(s => s.Sections).AsNoTracking()
            .Where(s => s.DeletedAt == null);

        if (TextMatching.ContainsPattern(query) is { } pattern)
        {
            // Lyrics keep their line breaks, so the stored text is flattened
            // the same way the query was: one space per run of whitespace.
            // Three passes of "  " -> " " flatten up to eight spaces in a row.
            songs = songs.Where(s =>
                EF.Functions.Like(s.Title, pattern)
                || EF.Functions.Like(s.Artist, pattern)
                || s.Sections.Any(x => EF.Functions.Like(
                    x.Text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")
                        .Replace("  ", " ").Replace("  ", " ").Replace("  ", " "),
                    pattern)));
        }

        var result = songs.OrderBy(s => s.Title).ToList();
        foreach (var song in result)
            song.Sections = [.. song.Sections.OrderBy(x => x.Order)];
        return result;
    }

    public IReadOnlyList<SongTitle> GetTitles()
    {
        using var db = new AppDbContext(dbPath);
        return db.Songs.AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .Select(s => new SongTitle(s.Id, s.Title, s.Artist))
            .ToList();
    }

    public Song? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Include(s => s.Sections).AsNoTracking()
            .FirstOrDefault(s => s.Id == id && s.DeletedAt == null);
        if (song is not null)
            song.Sections = [.. song.Sections.OrderBy(x => x.Order)];
        return song;
    }

    public Song Save(Song song)
    {
        using var db = new AppDbContext(dbPath);

        if (song.Id == 0)
        {
            db.Songs.Add(song);
            db.SaveChanges();
            return song;
        }

        var existing = db.Songs.Include(s => s.Sections).First(s => s.Id == song.Id);
        existing.Title = song.Title;
        existing.Artist = song.Artist;
        existing.Copyright = song.Copyright;
        ChangeTheme(db, existing, song.ThemeId);

        db.SongSections.RemoveRange(existing.Sections);
        existing.Sections = song.Sections
            .Select(x => new SongSection
            {
                Order = x.Order,
                Label = x.Label,
                Text = x.Text,
                StyleJson = x.StyleJson,
            })
            .ToList();

        db.SaveChanges();
        return Get(song.Id)!;
    }

    public void MarkProjected(int songId, DateTime when)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.FirstOrDefault(s => s.Id == songId);
        if (song is null)
            return;

        song.LastProjectedAt = when;
        db.SaveChanges();
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Find(id);
        if (song is null || song.DeletedAt is not null)
            return;
        song.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    public IReadOnlyList<Song> GetDeleted()
    {
        using var db = new AppDbContext(dbPath);
        return db.Songs.AsNoTracking()
            .Where(s => s.DeletedAt != null)
            .OrderByDescending(s => s.DeletedAt)
            .ToList();
    }

    public void Restore(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Find(id);
        if (song is null)
            return;
        song.DeletedAt = null;
        db.SaveChanges();
    }

    public void Purge(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Find(id);
        if (song is null || song.DeletedAt is null)
            return;
        db.Songs.Remove(song);
        db.Themes.RemoveRange(db.Themes.Where(t => t.OwnerSongId == id));
        db.SaveChanges();
    }

    public void PurgeOlderThan(DateTime limit)
    {
        using var db = new AppDbContext(dbPath);
        var old = db.Songs.Where(s => s.DeletedAt != null && s.DeletedAt < limit).ToList();
        if (old.Count == 0)
            return;
        var ids = old.Select(s => s.Id).ToList();
        db.Songs.RemoveRange(old);
        db.Themes.RemoveRange(db.Themes.Where(t => t.OwnerSongId != null && ids.Contains(t.OwnerSongId.Value)));
        db.SaveChanges();
    }

    public void SetTheme(int songId, int? themeId)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.FirstOrDefault(s => s.Id == songId);
        if (song is null)
            return;

        ChangeTheme(db, song, themeId);
        db.SaveChanges();
    }

    /// <summary>
    /// Sets the song's theme. When the song leaves its own theme, that theme
    /// goes too, or it would sit in the database belonging to nobody.
    /// </summary>
    private static void ChangeTheme(AppDbContext db, Song song, int? themeId)
    {
        if (song.ThemeId == themeId)
            return;

        if (song.ThemeId is int old
            && db.Themes.FirstOrDefault(t => t.Id == old && t.OwnerSongId == song.Id) is { } own)
            db.Themes.Remove(own);

        song.ThemeId = themeId;
    }
}
