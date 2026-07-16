namespace EcclesiaCast.Core.Songs;

public sealed class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Copyright { get; set; }
    public List<SongSection> Sections { get; set; } = [];
}
