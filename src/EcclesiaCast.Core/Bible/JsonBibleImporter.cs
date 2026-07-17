using System.Text.Json;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Imports Bible JSON files. Two shapes are recognized automatically:
///
/// 1. "Array of books" — the common shape used by most free Bible datasets:
///    <c>[{ "name": "Génesis", "chapters": [["v1", "v2", ...], ...] }, ...]</c>.
///    Book order in the array is assumed to be canonical (Génesis → Apocalipsis).
///
/// 2. "YouVersion-style export" — an object with a "books" array, where each
///    book carries a standard 3-letter USFM code ("GEN", "1CO", ...) and each
///    chapter's content is a list of typed items (headings, verses, ...); only
///    <c>"type": "verse"</c> items become slides.
/// </summary>
public static class JsonBibleImporter
{
    public static ParsedBible Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return ParseBookArray(root);

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("books", out var booksEl)
            && booksEl.ValueKind == JsonValueKind.Array)
            return ParseYouVersionExport(booksEl);

        throw new FormatException(
            "No se reconoce la estructura del JSON: se esperaba un arreglo de libros, " +
            "o un objeto con una propiedad \"books\".");
    }

    private static ParsedBible ParseBookArray(JsonElement root)
    {
        var books = new List<ParsedBook>();
        var number = 0;

        foreach (var bookEl in root.EnumerateArray())
        {
            number++;
            if (number > 66)
                break;

            var name = bookEl.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? nameEl.GetString() ?? string.Empty
                : string.Empty;
            if (name.Length == 0)
                name = BibleBookCatalog.FindByNumber(number)?.Name ?? $"Libro {number}";

            var verses = new List<ParsedVerse>();
            if (bookEl.TryGetProperty("chapters", out var chaptersEl) && chaptersEl.ValueKind == JsonValueKind.Array)
            {
                var chapter = 0;
                foreach (var chapterEl in chaptersEl.EnumerateArray())
                {
                    chapter++;
                    if (chapterEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var verse = 0;
                    foreach (var verseEl in chapterEl.EnumerateArray())
                    {
                        verse++;
                        var text = verseEl.ValueKind == JsonValueKind.String ? verseEl.GetString() ?? string.Empty : string.Empty;
                        if (text.Length > 0)
                            verses.Add(new ParsedVerse(chapter, verse, text));
                    }
                }
            }

            books.Add(new ParsedBook(number, name, verses));
        }

        return new ParsedBible(books);
    }

    private static ParsedBible ParseYouVersionExport(JsonElement booksEl)
    {
        var books = new List<ParsedBook>();

        foreach (var bookEl in booksEl.EnumerateArray())
        {
            var usfm = bookEl.TryGetProperty("book_usfm", out var usfmEl) && usfmEl.ValueKind == JsonValueKind.String
                ? usfmEl.GetString()
                : null;
            if (usfm is null || !UsfmBookCodes.TryGetValue(usfm, out var number))
                continue; // Libro no reconocido (introducciones, apéndices, etc.): se omite.

            var name = bookEl.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? nameEl.GetString() ?? string.Empty
                : string.Empty;
            if (name.Length == 0)
                name = BibleBookCatalog.FindByNumber(number)?.Name ?? $"Libro {number}";

            var verses = new List<ParsedVerse>();
            if (bookEl.TryGetProperty("chapters", out var chaptersEl) && chaptersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var chapterEl in chaptersEl.EnumerateArray())
                {
                    var chapterNumber = ExtractChapterNumber(chapterEl);
                    if (chapterNumber <= 0
                        || !chapterEl.TryGetProperty("items", out var itemsEl)
                        || itemsEl.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var itemEl in itemsEl.EnumerateArray())
                    {
                        if (!itemEl.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "verse")
                            continue;
                        if (!itemEl.TryGetProperty("verse_numbers", out var numbersEl)
                            || numbersEl.ValueKind != JsonValueKind.Array)
                            continue;

                        var text = ExtractLines(itemEl);
                        if (text.Length == 0)
                            continue;

                        // A verse can cover more than one number (e.g. combined
                        // verses printed together); the text is stored under each.
                        foreach (var numEl in numbersEl.EnumerateArray())
                        {
                            if (numEl.TryGetInt32(out var verseNumber) && verseNumber > 0)
                                verses.Add(new ParsedVerse(chapterNumber, verseNumber, text));
                        }
                    }
                }
            }

            books.Add(new ParsedBook(number, name, verses));
        }

        books.Sort((a, b) => a.Number.CompareTo(b.Number));
        return new ParsedBible(books);
    }

    private static int ExtractChapterNumber(JsonElement chapterEl)
    {
        if (!chapterEl.TryGetProperty("chapter_usfm", out var usfmEl) || usfmEl.ValueKind != JsonValueKind.String)
            return 0;

        var value = usfmEl.GetString();
        var dot = value?.LastIndexOf('.') ?? -1;
        return dot >= 0 && int.TryParse(value.AsSpan(dot + 1), out var n) ? n : 0;
    }

    private static string ExtractLines(JsonElement itemEl)
    {
        if (!itemEl.TryGetProperty("lines", out var linesEl) || linesEl.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var parts = new List<string>();
        foreach (var lineEl in linesEl.EnumerateArray())
        {
            if (lineEl.ValueKind != JsonValueKind.String)
                continue;
            var text = lineEl.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text.Trim());
        }

        return string.Join(" ", parts);
    }

    /// <summary>Standard 3-letter USFM codes, in canonical order (Génesis = 1 … Apocalipsis = 66).</summary>
    private static readonly IReadOnlyDictionary<string, int> UsfmBookCodes = BuildUsfmCodes();

    private static Dictionary<string, int> BuildUsfmCodes()
    {
        string[] codes =
        [
            "GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA",
            "1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST", "JOB", "PSA", "PRO",
            "ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS", "JOL", "AMO",
            "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
            "MAT", "MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH",
            "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM", "HEB", "JAS",
            "1PE", "2PE", "1JN", "2JN", "3JN", "JUD", "REV",
        ];

        var map = new Dictionary<string, int>(codes.Length);
        for (var i = 0; i < codes.Length; i++)
            map[codes[i]] = i + 1;
        return map;
    }
}
