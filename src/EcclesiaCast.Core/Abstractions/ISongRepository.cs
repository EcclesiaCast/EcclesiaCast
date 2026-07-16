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
}
