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

    /// <summary>Deletes a song, and its own theme with it.</summary>
    void Delete(int id);

    /// <summary>
    /// Points a song at another theme (null: the main one) without touching
    /// its lyrics. Like <see cref="Save"/>, an own theme the song leaves
    /// behind is deleted: nothing else can use it.
    /// </summary>
    void SetTheme(int songId, int? themeId);

    /// <summary>
    /// Notes that a song went on the projector. Called every time a slide of
    /// it goes live, so it stays cheap: one column, no sections.
    /// </summary>
    void MarkProjected(int songId, DateTime when);
}
