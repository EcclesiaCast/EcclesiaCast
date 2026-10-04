namespace EcclesiaCast.Core.Songs.Web;

/// <summary>Where a song found on the internet came from.</summary>
public enum WebSongSource
{
    MusicaCom,
    Lrclib,
}

/// <summary>
/// One search result. Results from LRCLIB already carry the lyrics; those
/// from musica.com only point at the page, which is read when the operator
/// picks the song.
/// </summary>
public sealed record WebSongHit(
    WebSongSource Source,
    string Title,
    string Artist,
    string Url,
    string? Lyrics = null)
{
    public string SourceName => Source switch
    {
        WebSongSource.MusicaCom => "musica.com",
        WebSongSource.Lrclib => "lrclib.net",
        _ => Source.ToString(),
    };
}

/// <summary>A song read from the internet, with its lyrics in editor format.</summary>
public sealed record WebSong(string Title, string Artist, string Lyrics);
