using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Media;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class MediaRepository(string dbPath) : IMediaRepository
{
    public IReadOnlyList<MediaItem> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        return db.MediaItems.AsNoTracking().OrderBy(m => m.Id).ToList();
    }

    public MediaItem Add(MediaItem item)
    {
        using var db = new AppDbContext(dbPath);
        db.MediaItems.Add(item);
        db.SaveChanges();
        return item;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var item = db.MediaItems.Find(id);
        if (item is null)
            return;
        db.MediaItems.Remove(item);
        db.SaveChanges();
    }
}
