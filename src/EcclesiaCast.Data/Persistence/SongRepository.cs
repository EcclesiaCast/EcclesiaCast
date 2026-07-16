using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class SongRepository(string dbPath) : ISongRepository
{
    public IReadOnlyList<Song> Search(string? query = null)
    {
        using var db = new AppDbContext(dbPath);
        IQueryable<Song> songs = db.Songs.Include(s => s.Sections).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            songs = songs.Where(s =>
                EF.Functions.Like(s.Title, pattern)
                || EF.Functions.Like(s.Artist, pattern)
                || s.Sections.Any(x => EF.Functions.Like(x.Text, pattern)));
        }

        var result = songs.OrderBy(s => s.Title).ToList();
        foreach (var song in result)
            song.Sections = [.. song.Sections.OrderBy(x => x.Order)];
        return result;
    }

    public Song? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Include(s => s.Sections).AsNoTracking()
            .FirstOrDefault(s => s.Id == id);
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

        db.SongSections.RemoveRange(existing.Sections);
        existing.Sections = song.Sections
            .Select(x => new SongSection { Order = x.Order, Label = x.Label, Text = x.Text })
            .ToList();

        db.SaveChanges();
        return Get(song.Id)!;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var song = db.Songs.Find(id);
        if (song is null)
            return;
        db.Songs.Remove(song);
        db.SaveChanges();
    }
}
