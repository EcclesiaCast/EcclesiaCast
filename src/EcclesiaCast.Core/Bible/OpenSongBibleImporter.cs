using System.Xml.Linq;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Imports the OpenSong XML Bible format, the one that ProPresenter, Holyrics,
/// FreeShow and many Bible download sites (Beblia among them) hand out:
/// <c>&lt;bible&gt;&lt;b n="Génesis"&gt;&lt;c n="1"&gt;&lt;v n="1"&gt;...&lt;/v&gt;&lt;/c&gt;&lt;/b&gt;&lt;/bible&gt;</c>.
/// Also the longer spelling of the same tree that Beblia uses,
/// <c>&lt;bible&gt;&lt;testament&gt;&lt;book number="1"&gt;&lt;chapter number="1"&gt;&lt;verse number="1"&gt;</c>.
/// </summary>
/// <remarks>
/// In OpenSong <c>n</c> of a book is its name, in whatever language, so the
/// number comes from: a number in the file if there is one, else the position
/// when the file has the 66 books, else the name (Spanish names and the usual
/// abbreviations), else the position (counting from Mateo when the file has
/// only the 27 books of the New Testament).
/// </remarks>
public static class OpenSongBibleImporter
{
    private const int NewTestamentBooks = 27;
    private const int FirstNewTestamentBook = 40;

    public static ParsedBible Parse(string xml) => Parse(XDocument.Parse(xml));

    internal static ParsedBible Parse(XDocument doc)
    {
        // Books hang from the root, or from a <testament> in Beblia; a <b> lower
        // down would be bold text inside a verse.
        var bookElements = doc.Descendants()
            .Where(e => Is(e, "b", "book")
                && (e.Parent == doc.Root || (e.Parent is { } parent && Is(parent, "testament", "testament"))))
            .ToList();

        var books = new Dictionary<int, ParsedBook>();
        for (var i = 0; i < bookElements.Count; i++)
        {
            var bookEl = bookElements[i];
            var fileName = Attr(bookEl, "n", "name", "bname")?.Trim();
            var number = BookNumber(bookEl, fileName, i, bookElements.Count);
            if (number is < 1 or > 66 || books.ContainsKey(number))
                continue;

            var name = string.IsNullOrWhiteSpace(fileName) || int.TryParse(fileName, out _)
                ? BibleBookCatalog.FindByNumber(number)?.Name ?? $"Libro {number}"
                : fileName;

            var verses = new List<ParsedVerse>();
            foreach (var chapterEl in bookEl.Elements().Where(e => Is(e, "c", "chapter")))
            {
                var chapter = LeadingNumber(Attr(chapterEl, "n", "number", "cnumber"));
                if (chapter <= 0)
                    continue;

                foreach (var verseEl in chapterEl.Elements().Where(e => Is(e, "v", "verse")))
                {
                    var verse = LeadingNumber(Attr(verseEl, "n", "number", "vnumber"));
                    var text = verseEl.Value.Trim();
                    if (verse > 0 && text.Length > 0)
                        verses.Add(new ParsedVerse(chapter, verse, text));
                }
            }

            books[number] = new ParsedBook(number, name, verses);
        }

        return new ParsedBible(books.Values.OrderBy(b => b.Number).ToList());
    }

    private static int BookNumber(XElement bookEl, string? fileName, int index, int count)
    {
        var explicitNumber = LeadingNumber(Attr(bookEl, "number", "bnumber", "id"));
        if (explicitNumber > 0)
            return explicitNumber;
        if (fileName is not null && int.TryParse(fileName, out var named))
            return named;
        if (count == 66)
            return index + 1;
        if (fileName is not null && BibleBookCatalog.FindByName(fileName) is { } known)
            return known.Number;
        return count == NewTestamentBooks ? FirstNewTestamentBook + index : index + 1;
    }

    private static bool Is(XElement element, string shortName, string longName) =>
        string.Equals(element.Name.LocalName, shortName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(element.Name.LocalName, longName, StringComparison.OrdinalIgnoreCase);

    private static string? Attr(XElement element, params string[] names)
    {
        foreach (var attribute in element.Attributes())
            if (names.Any(n => string.Equals(attribute.Name.LocalName, n, StringComparison.OrdinalIgnoreCase)))
                return (string)attribute;
        return null;
    }

    /// <summary>"16" → 16; "16-17" (a joined verse) → 16; anything else → 0.</summary>
    private static int LeadingNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        var digits = new string(text.Trim().TakeWhile(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }
}
