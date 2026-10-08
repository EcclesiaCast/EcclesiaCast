using System.Text.Json;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Writes a Bible in the "array of books" shape that <see cref="JsonBibleImporter"/>
/// reads back, so an installed version can be moved to another computer.
/// The importer takes the position in the array as the book number and the
/// position in a chapter as the verse number, so every one of the 66 books is
/// written (missing ones empty) and the gaps inside a chapter stay as empty
/// strings, which the importer skips.
/// </summary>
public static class JsonBibleExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // Keep accents and quotes readable instead of ó escapes.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(ParsedBible bible)
    {
        var books = new List<object>(66);
        for (var number = 1; number <= 66; number++)
        {
            var book = bible.Books.FirstOrDefault(b => b.Number == number);
            var name = book?.Name is { Length: > 0 } n
                ? n
                : BibleBookCatalog.FindByNumber(number)?.Name ?? $"Libro {number}";
            books.Add(new { name, chapters = BuildChapters(book) });
        }

        return JsonSerializer.Serialize(books, Options);
    }

    private static List<List<string>> BuildChapters(ParsedBook? book)
    {
        var chapters = new List<List<string>>();
        if (book is null || book.Verses.Count == 0)
            return chapters;

        var lastChapter = book.Verses.Max(v => v.Chapter);
        for (var chapter = 1; chapter <= lastChapter; chapter++)
        {
            var verses = book.Verses.Where(v => v.Chapter == chapter).ToList();
            var texts = new List<string>();
            if (verses.Count > 0)
            {
                var lastVerse = verses.Max(v => v.Verse);
                for (var verse = 1; verse <= lastVerse; verse++)
                {
                    // Two rows with the same number (a malformed source) keep the first.
                    texts.Add(verses.FirstOrDefault(v => v.Verse == verse)?.Text ?? string.Empty);
                }
            }
            chapters.Add(texts);
        }

        return chapters;
    }
}
