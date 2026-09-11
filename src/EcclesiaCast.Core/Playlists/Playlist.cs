namespace EcclesiaCast.Core.Playlists;

public enum PlaylistItemType
{
    Song,
    BiblePassage,
    Media,
}

/// <summary>
/// How a smart playlist decides what belongs in it. A church that has been
/// going for years accumulates songs faster than anyone can remember them.
/// </summary>
public enum PlaylistRule
{
    /// <summary>Not smart: the operator puts the items in by hand.</summary>
    None,

    /// <summary>Songs that joined the library within the last N days.</summary>
    RecentlyAdded,

    /// <summary>Songs not projected in the last N days — the forgotten ones.</summary>
    NotSungLately,

    /// <summary>Songs by one artist.</summary>
    ByArtist,

    /// <summary>Songs whose title or lyrics contain a word.</summary>
    Containing,
}

/// <summary>The order of a service: songs, passages, and media, in sequence.</summary>
public sealed class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PlaylistItem> Items { get; set; } = [];

    /// <summary>
    /// When set, the list fills itself from the library instead of being
    /// arranged by hand, and its items are not stored.
    /// </summary>
    public PlaylistRule Rule { get; set; }

    /// <summary>The rule's parameter: a number of days, an artist, a word.</summary>
    public string? RuleValue { get; set; }

    public bool IsSmart => Rule != PlaylistRule.None;
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
