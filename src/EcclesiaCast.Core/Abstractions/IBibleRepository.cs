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

    /// <summary>
    /// Changes a version's name and abbreviation. Playlist passages carry the
    /// abbreviation in their caption («Juan 3:16 (RVR1960)»), so they follow.
    /// </summary>
    void UpdateVersion(int versionId, string name, string abbreviation);

    void DeleteVersion(int versionId);

    /// <summary>
    /// Every book and verse of a version as it would be parsed from a file —
    /// what Export writes, and what importing the file again gives back.
    /// </summary>
    ParsedBible ReadVersion(int versionId);

    /// <summary>Canonical book numbers (1–66) present in this version, in Bible order.</summary>
    IReadOnlyList<int> GetAvailableBookNumbers(int versionId);

    /// <summary>Chapter numbers available for a book in this version, in order.</summary>
    IReadOnlyList<int> GetChapterNumbers(int versionId, int bookNumber);

    /// <summary>Verses for a reference: a single verse, a range, or the whole chapter (null start).</summary>
    IReadOnlyList<BibleVerseResult> GetPassage(int versionId, BibleReference reference);

    IReadOnlyList<BibleVerseResult> SearchText(int versionId, string query, int limit = 60);
}
