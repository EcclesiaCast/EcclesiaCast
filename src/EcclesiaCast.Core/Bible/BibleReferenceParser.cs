using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Parses references typed by the operator: "Juan 3:16", "jn 3:16-18",
/// "1 co 13" (whole chapter), "sal 23". Returns null for anything that
/// isn't a recognizable reference, so callers can fall back to a text
/// search instead.
/// </summary>
public static partial class BibleReferenceParser
{
    [GeneratedRegex(@"^\s*(?<book>.+?)\s+(?<chapter>\d{1,3})(?:\s*:\s*(?<vstart>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3}))?)?\s*$")]
    private static partial Regex Pattern();

    public static BibleReference? TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var match = Pattern().Match(input.Trim());
        if (!match.Success)
            return null;

        var book = BibleBookCatalog.FindByName(match.Groups["book"].Value);
        if (book is null)
            return null;

        var chapter = int.Parse(match.Groups["chapter"].Value);
        if (chapter <= 0)
            return null;

        int? verseStart = match.Groups["vstart"].Success
            ? int.Parse(match.Groups["vstart"].Value)
            : null;
        if (verseStart is 0)
            return null;

        int? verseEnd = match.Groups["vend"].Success
            ? int.Parse(match.Groups["vend"].Value)
            : verseStart;
        if (verseEnd is not null && verseStart is not null && verseEnd < verseStart)
            return null;

        return new BibleReference(book.Number, chapter, verseStart, verseEnd);
    }
}
