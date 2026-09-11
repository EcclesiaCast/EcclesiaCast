namespace EcclesiaCast.Core.Songs;

public sealed class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Copyright { get; set; }

    /// <summary>Theme override for this song; null uses the default song theme.</summary>
    public int? ThemeId { get; set; }

    /// <summary>When the song joined the library, for the "recently added" list.</summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Last time this song went on the projector, or null if it never has.
    /// It is what lets the church find the songs it has stopped singing.
    /// </summary>
    public DateTime? LastProjectedAt { get; set; }

    public List<SongSection> Sections { get; set; } = [];
}
