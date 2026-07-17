namespace EcclesiaCast.Core.Bible;

/// <summary>A book as it appears in one imported version (its own display name).</summary>
public sealed class BibleBook
{
    public int Id { get; set; }
    public int VersionId { get; set; }

    /// <summary>Canonical number 1–66; see <see cref="BibleBookCatalog"/>.</summary>
    public int Number { get; set; }

    public string Name { get; set; } = string.Empty;
    public List<BibleVerse> Verses { get; set; } = [];
}
