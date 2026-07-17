namespace EcclesiaCast.Core.Bible;

/// <summary>One verse as read from a source file, before persistence.</summary>
public sealed record ParsedVerse(int Chapter, int Verse, string Text);

/// <summary>One book as read from a source file, before persistence.</summary>
public sealed record ParsedBook(int Number, string Name, List<ParsedVerse> Verses);

/// <summary>The result of parsing a Bible file, before it is saved as a version.</summary>
public sealed record ParsedBible(List<ParsedBook> Books)
{
    public int VerseCount => Books.Sum(b => b.Verses.Count);

    /// <summary>Canonical books (1–66) with no verses at all — reported to the operator after import.</summary>
    public IReadOnlyList<int> MissingBookNumbers =>
        Enumerable.Range(1, 66)
            .Except(Books.Where(b => b.Verses.Count > 0).Select(b => b.Number))
            .ToList();
}
