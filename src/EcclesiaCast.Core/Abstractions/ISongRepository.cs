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

    /// <summary>Id and title of every song, without their lyrics: the cheap way to ask whether a title is taken.</summary>
    IReadOnlyList<SongTitle> GetTitles();

    /// <summary>Inserts (Id 0) or updates a song, replacing its sections.</summary>
    Song Save(Song song);

    /// <summary>Sends the song to the trash (it is not erased: see <see cref="Purge"/>).</summary>
    void Delete(int id);

    /// <summary>The songs in the trash, the most recently deleted first.</summary>
    IReadOnlyList<Song> GetDeleted();

    /// <summary>Takes a song back out of the trash.</summary>
    void Restore(int id);

    /// <summary>Erases a trashed song for good.</summary>
    void Purge(int id);

    /// <summary>Erases for good what was trashed before <paramref name="limit"/> (UTC).</summary>
    void PurgeOlderThan(DateTime limit);

    /// <summary>
    /// Notes that a song went on the projector. Called every time a slide of
    /// it goes live, so it stays cheap: one column, no sections.
    /// </summary>
    void MarkProjected(int songId, DateTime when);
}
