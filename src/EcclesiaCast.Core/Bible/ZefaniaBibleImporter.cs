using System.Xml.Linq;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Imports the Zefania XML Bible format:
/// <c>&lt;XMLBIBLE&gt;&lt;BIBLEBOOK bnumber="1" bname="Genesis"&gt;&lt;CHAPTER cnumber="1"&gt;&lt;VERS vnumber="1"&gt;...&lt;/VERS&gt;...&lt;/CHAPTER&gt;...&lt;/BIBLEBOOK&gt;...&lt;/XMLBIBLE&gt;</c>.
/// <c>bnumber</c> is already the canonical 1–66 book number.
/// </summary>
public static class ZefaniaBibleImporter
{
    public static ParsedBible Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var books = new List<ParsedBook>();

        foreach (var bookEl in doc.Descendants("BIBLEBOOK"))
        {
            var number = (int?)bookEl.Attribute("bnumber") ?? 0;
            if (number is < 1 or > 66)
                continue;

            var name = (string?)bookEl.Attribute("bname");
            if (string.IsNullOrWhiteSpace(name))
                name = BibleBookCatalog.FindByNumber(number)?.Name ?? $"Libro {number}";

            var verses = new List<ParsedVerse>();
            foreach (var chapterEl in bookEl.Elements("CHAPTER"))
            {
                var chapter = (int?)chapterEl.Attribute("cnumber") ?? 0;
                if (chapter <= 0)
                    continue;

                foreach (var versEl in chapterEl.Elements("VERS"))
                {
                    var verse = (int?)versEl.Attribute("vnumber") ?? 0;
                    var text = versEl.Value.Trim();
                    if (verse > 0 && text.Length > 0)
                        verses.Add(new ParsedVerse(chapter, verse, text));
                }
            }

            books.Add(new ParsedBook(number, name!, verses));
        }

        books.Sort((a, b) => a.Number.CompareTo(b.Number));
        return new ParsedBible(books);
    }
}
