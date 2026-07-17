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

    /// <summary>Verses for a reference: a single verse, a range, or the whole chapter (null start).</summary>
    IReadOnlyList<BibleVerseResult> GetPassage(int versionId, BibleReference reference);

    IReadOnlyList<BibleVerseResult> SearchText(int versionId, string query, int limit = 60);
}
