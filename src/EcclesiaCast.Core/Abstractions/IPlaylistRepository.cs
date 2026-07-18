using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.Core.Abstractions;

public interface IPlaylistRepository
{
    /// <summary>All playlists with their items, ordered by name.</summary>
    IReadOnlyList<Playlist> GetAll();

    Playlist? Get(int id);

    /// <summary>Inserts (Id 0) or updates a playlist, replacing its items.</summary>
    Playlist Save(Playlist playlist);

    void Delete(int id);
}
