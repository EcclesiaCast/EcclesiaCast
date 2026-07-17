using System.Text.Json;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Imports the common "array of books" JSON shape used by most free Bible
/// datasets: <c>[{ "name": "Génesis", "chapters": [["v1", "v2", ...], ...] }, ...]</c>.
/// Book order in the array is assumed to be canonical (Génesis → Apocalipsis);
/// an "abbrev" property, if present, is ignored in favor of array position.
/// </summary>
public static class JsonBibleImporter
{
    public static ParsedBible Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
            throw new FormatException("Se esperaba un arreglo de libros en el JSON.");

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
}
