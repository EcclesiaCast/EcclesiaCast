namespace EcclesiaCast.Core.Songs;

public sealed class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Copyright { get; set; }

    /// <summary>Theme override for this song; null uses the default song theme.</summary>
    public int? ThemeId { get; set; }

    public List<SongSection> Sections { get; set; } = [];
}
