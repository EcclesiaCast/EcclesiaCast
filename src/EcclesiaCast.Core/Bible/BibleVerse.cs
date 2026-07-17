namespace EcclesiaCast.Core.Bible;

public sealed class BibleVerse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public BibleBook Book { get; set; } = null!;
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Text { get; set; } = string.Empty;
}
