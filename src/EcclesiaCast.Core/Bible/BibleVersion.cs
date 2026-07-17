namespace EcclesiaCast.Core.Bible;

public sealed class BibleVersion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public List<BibleBook> Books { get; set; } = [];
}
