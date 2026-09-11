using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.Core.Abstractions;

public interface ISongRepository
{
    /// <summary>
    /// Songs matching the query in title, artist or lyrics, ordered by
    /// title. A null or empty query returns the whole library.
    /// </summary>
    IReadOnlyList<Song> Search(string? query = null);

    Song? Get(int id);

    /// <summary>Inserts (Id 0) or updates a song, replacing its sections.</summary>
    Song Save(Song song);

    void Delete(int id);

    /// <summary>
    /// Notes that a song went on the projector. Called every time a slide of
    /// it goes live, so it stays cheap: one column, no sections.
    /// </summary>
    void MarkProjected(int songId, DateTime when);
}
