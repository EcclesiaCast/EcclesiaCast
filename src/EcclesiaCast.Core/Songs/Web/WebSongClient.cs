using System.Net;

namespace EcclesiaCast.Core.Songs.Web;

/// <summary>
/// Looks songs up on the internet: musica.com first, LRCLIB as the fallback.
///
/// The full musica.com search (the one that finds a song by a line of its
/// lyrics) runs inside a browser and lives in the app; this class covers
/// what plain HTTP can reach.
/// </summary>
public sealed class WebSongClient : IDisposable
{
    private readonly HttpClient _http;

    public WebSongClient()
    {
        _http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        // LRCLIB asks clients to say who they are; musica.com only answers
        // to something that looks like a browser.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) EcclesiaCast/1.0 (+https://github.com/EcclesiaCast/EcclesiaCast)");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es");
    }

    /// <summary>musica.com by title or artist.</summary>
    public async Task<IReadOnlyList<WebSongHit>> SearchMusicaComAsync(string query, CancellationToken cancel = default)
    {
        var html = await _http.GetStringAsync(MusicaComParser.QuickSearchUrl(query), cancel);
        return MusicaComParser.ParseQuickSearch(html);
    }

    /// <summary>LRCLIB by title, artist or album. It retries once when the server is busy.</summary>
    public async Task<IReadOnlyList<WebSongHit>> SearchLrclibAsync(string query, CancellationToken cancel = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var response = await _http.GetAsync(LrclibParser.SearchUrl(query), cancel);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 2)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5), cancel);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return LrclibParser.ParseSearch(await response.Content.ReadAsStringAsync(cancel));
        }
    }

    /// <summary>The song behind a result, or null if its page has no lyrics.</summary>
    public async Task<WebSong?> GetSongAsync(WebSongHit hit, CancellationToken cancel = default)
    {
        if (hit.Lyrics is not null)
            return new WebSong(hit.Title, hit.Artist, hit.Lyrics);

        var html = await _http.GetStringAsync(hit.Url, cancel);
        var song = MusicaComParser.ParseLyricsPage(html);
        if (song is null)
            return null;

        // The page names are the canonical ones, but if one is missing keep what the search said.
        return song with
        {
            Title = song.Title.Length > 0 ? song.Title : hit.Title,
            Artist = song.Artist.Length > 0 ? song.Artist : hit.Artist,
        };
    }

    public void Dispose() => _http.Dispose();
}
