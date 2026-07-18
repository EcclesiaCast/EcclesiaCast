using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Playlists;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class PlaylistRepository(string dbPath) : IPlaylistRepository
{
    public IReadOnlyList<Playlist> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        var playlists = db.Playlists.Include(p => p.Items).AsNoTracking()
            .OrderBy(p => p.Name)
            .ToList();
        foreach (var playlist in playlists)
            playlist.Items = [.. playlist.Items.OrderBy(i => i.Order)];
        return playlists;
    }

    public Playlist? Get(int id)
    {
        using var db = new AppDbContext(dbPath);
        var playlist = db.Playlists.Include(p => p.Items).AsNoTracking()
            .FirstOrDefault(p => p.Id == id);
        if (playlist is not null)
            playlist.Items = [.. playlist.Items.OrderBy(i => i.Order)];
        return playlist;
    }

    public Playlist Save(Playlist playlist)
    {
        using var db = new AppDbContext(dbPath);

        if (playlist.Id == 0)
        {
            db.Playlists.Add(playlist);
            db.SaveChanges();
            return playlist;
        }

        var existing = db.Playlists.Include(p => p.Items).First(p => p.Id == playlist.Id);
        existing.Name = playlist.Name;
        db.PlaylistItems.RemoveRange(existing.Items);
        existing.Items = playlist.Items
            .Select(i => new PlaylistItem
            {
                Order = i.Order,
                Type = i.Type,
                Caption = i.Caption,
                SongId = i.SongId,
                BibleVersionId = i.BibleVersionId,
                BookNumber = i.BookNumber,
                Chapter = i.Chapter,
                VerseStart = i.VerseStart,
                VerseEnd = i.VerseEnd,
                MediaId = i.MediaId,
            })
            .ToList();
        db.SaveChanges();
        return Get(playlist.Id)!;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var playlist = db.Playlists.Find(id);
        if (playlist is null)
            return;
        db.Playlists.Remove(playlist);
        db.SaveChanges();
    }
}
