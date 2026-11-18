namespace EcclesiaCast.Core.Songs;

public sealed class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Copyright { get; set; }

    /// <summary>
    /// The theme this song uses: null follows the main song theme; otherwise a
    /// shared theme chosen for it, or the song's own theme (one whose
    /// <see cref="Themes.SlideTheme.OwnerSongId"/> is this song).
    /// </summary>
    public int? ThemeId { get; set; }

    /// <summary>When the song joined the library, for the "recently added" list.</summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Last time this song went on the projector, or null if it never has.
    /// It is what lets the church find the songs it has stopped singing.
    /// </summary>
    public DateTime? LastProjectedAt { get; set; }

    /// <summary>When the song went to the trash (UTC); null while it is in the library.</summary>
    public DateTime? DeletedAt { get; set; }

    public List<SongSection> Sections { get; set; } = [];
}
