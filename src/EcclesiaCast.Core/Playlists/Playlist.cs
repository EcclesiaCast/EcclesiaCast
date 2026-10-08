namespace EcclesiaCast.Core.Playlists;

public enum PlaylistItemType
{
    Song,
    BiblePassage,
    Media,
}

/// <summary>The order of a service: songs, passages, and media, in sequence.</summary>
public sealed class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PlaylistItem> Items { get; set; } = [];
}

public sealed class PlaylistItem
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int Order { get; set; }
    public PlaylistItemType Type { get; set; }

    /// <summary>Label shown to the operator (song title, reference, media name).</summary>
    public string Caption { get; set; } = string.Empty;

    // Song
    public int? SongId { get; set; }

    // Bible passage
    public int? BibleVersionId { get; set; }
    public int? BookNumber { get; set; }
    public int? Chapter { get; set; }
    public int? VerseStart { get; set; }
    public int? VerseEnd { get; set; }

    // Media
    public int? MediaId { get; set; }
}
