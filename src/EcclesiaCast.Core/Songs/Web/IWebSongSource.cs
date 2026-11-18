namespace EcclesiaCast.Core.Songs.Web;

/// <summary>Where songs are looked up on the internet (<see cref="WebSongClient"/>; the tests use a made-up one).</summary>
public interface IWebSongSource : IDisposable
{
    /// <summary>musica.com by title or artist.</summary>
    Task<IReadOnlyList<WebSongHit>> SearchMusicaComAsync(string query, CancellationToken cancel = default);

    /// <summary>LRCLIB, which also carries the lyrics.</summary>
    Task<IReadOnlyList<WebSongHit>> SearchLrclibAsync(string query, CancellationToken cancel = default);

    /// <summary>The song behind a result, or null if its page has no lyrics.</summary>
    Task<WebSong?> GetSongAsync(WebSongHit hit, CancellationToken cancel = default);
}
