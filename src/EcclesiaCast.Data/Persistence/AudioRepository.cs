using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Audio;
using Microsoft.EntityFrameworkCore;

namespace EcclesiaCast.Data.Persistence;

public sealed class AudioRepository(string dbPath) : IAudioRepository
{
    public IReadOnlyList<AudioTrack> GetAll()
    {
        using var db = new AppDbContext(dbPath);
        return db.AudioTracks.AsNoTracking().OrderBy(t => t.Order).ThenBy(t => t.Id).ToList();
    }

    public AudioTrack Add(AudioTrack track)
    {
        using var db = new AppDbContext(dbPath);
        track.Order = db.AudioTracks.Any() ? db.AudioTracks.Max(t => t.Order) + 1 : 0;
        db.AudioTracks.Add(track);
        db.SaveChanges();
        return track;
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext(dbPath);
        var track = db.AudioTracks.Find(id);
        if (track is null)
            return;

        db.AudioTracks.Remove(track);
        db.SaveChanges();
    }

    public void SaveOrder(IReadOnlyList<int> idsInOrder)
    {
        using var db = new AppDbContext(dbPath);
        var tracks = db.AudioTracks.ToDictionary(t => t.Id);
        for (var i = 0; i < idsInOrder.Count; i++)
        {
            if (tracks.TryGetValue(idsInOrder[i], out var track))
                track.Order = i;
        }

        db.SaveChanges();
    }
}
