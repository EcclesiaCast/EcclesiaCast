using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// A verse the operator projected from the Bible search, with the versions it
/// was read in, so the history can bring it back just as it was.
/// </summary>
public sealed record BibleHistoryEntry(
    int BookNumber,
    int Chapter,
    int VerseStart,
    int? VerseEnd,
    int? VersionId,
    string VersionAbbreviation,
    int? SecondaryVersionId,
    DateTimeOffset At)
{
    /// <summary>The reference as it is typed: «Juan 3:16-18».</summary>
    [JsonIgnore]
    public string Reference => BibleReferenceFormatter.Format(ToReference());

    /// <summary>What the history list shows: «Juan 3:16 · NTV».</summary>
    [JsonIgnore]
    public string Display =>
        string.IsNullOrWhiteSpace(VersionAbbreviation) ? Reference : $"{Reference} · {VersionAbbreviation.Trim()}";

    public BibleReference ToReference() => new(BookNumber, Chapter, VerseStart, VerseEnd);

    /// <summary>The same passage, whatever the version it was read in.</summary>
    internal bool SamePassage(BibleHistoryEntry other) =>
        BookNumber == other.BookNumber
        && Chapter == other.Chapter
        && VerseStart == other.VerseStart
        && (VerseEnd ?? VerseStart) == (other.VerseEnd ?? other.VerseStart);
}

/// <summary>
/// The list of verses searched and projected, newest first, kept as a short
/// JSON text in the settings.
/// </summary>
public static class BibleHistoryList
{
    public const int DefaultCapacity = 30;

    /// <summary>
    /// The list with the entry on top. A passage already there (same book,
    /// chapter and verses, in whatever version) is replaced, not repeated;
    /// the oldest fall off past <paramref name="capacity"/>.
    /// </summary>
    public static IReadOnlyList<BibleHistoryEntry> Add(
        IEnumerable<BibleHistoryEntry> list, BibleHistoryEntry entry, int capacity = DefaultCapacity)
    {
        var result = new List<BibleHistoryEntry> { entry };
        result.AddRange(list.Where(existing => !existing.SamePassage(entry)));

        return capacity >= 0 && result.Count > capacity ? result.Take(capacity).ToList() : result;
    }

    public static string Serialize(IEnumerable<BibleHistoryEntry> list) => JsonSerializer.Serialize(list);

    /// <summary>
    /// Reads what <see cref="Serialize"/> wrote. Nothing, damaged text or an
    /// entry that makes no sense (a book outside 1..66, a chapter or verse
    /// under 1) never fails: those are left out.
    /// </summary>
    public static IReadOnlyList<BibleHistoryEntry> Parse(string? text, int capacity = DefaultCapacity)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        List<BibleHistoryEntry?>? read;
        try
        {
            read = JsonSerializer.Deserialize<List<BibleHistoryEntry?>>(text);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return [];
        }

        if (read is null)
            return [];

        return read
            .Where(IsSensible)
            .Select(e => e!)
            .Take(capacity)
            .ToList();
    }

    private static bool IsSensible(BibleHistoryEntry? entry) =>
        entry is { BookNumber: >= 1 and <= 66, Chapter: >= 1, VerseStart: >= 1 }
        && (entry.VerseEnd is null || entry.VerseEnd >= entry.VerseStart);
}
