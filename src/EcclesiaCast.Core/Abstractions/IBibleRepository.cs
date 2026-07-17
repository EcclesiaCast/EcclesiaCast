using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Core.Abstractions;

public sealed record BibleVersionInfo(int Id, string Name, string Abbreviation, string Language, int VerseCount);

public sealed record BibleVerseResult(int BookNumber, string BookName, int Chapter, int Verse, string Text)
{
    public string Reference => $"{BookName} {Chapter}:{Verse}";
}

public interface IBibleRepository
{
    IReadOnlyList<BibleVersionInfo> GetVersions();

    BibleVersionInfo Import(string name, string abbreviation, string language, ParsedBible parsed);

    void RenameVersion(int versionId, string newName);

    void DeleteVersion(int versionId);

    /// <summary>Canonical book numbers (1–66) present in this version, in Bible order.</summary>
    IReadOnlyList<int> GetAvailableBookNumbers(int versionId);

    /// <summary>Chapter numbers available for a book in this version, in order.</summary>
    IReadOnlyList<int> GetChapterNumbers(int versionId, int bookNumber);

    /// <summary>Verses for a reference: a single verse, a range, or the whole chapter (null start).</summary>
    IReadOnlyList<BibleVerseResult> GetPassage(int versionId, BibleReference reference);

    IReadOnlyList<BibleVerseResult> SearchText(int versionId, string query, int limit = 60);
}
