using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using EcclesiaCast.Core.Bible;

// Generador de las Biblias para bajar desde el programa. Tres órdenes:
//
//   usfx  <archivo _usfx.zip o .xml> <nombre> <sigla> <idioma> <salida.ecbiblias>
//         Convierte una Biblia de eBible.org (USFX) en un paquete de una versión.
//         Toma sólo los 66 libros; deja afuera notas, referencias y títulos.
//   separar <carpeta de salida> <paquete.ecbiblias>...
//         Escribe un .ecbiblias por cada versión de los paquetes (SIGLA.ecbiblias).
//   lista <carpeta> [SIGLA=licencia]...
//         Escribe catalogo.json con todos los .ecbiblias de la carpeta: nombre,
//         sigla, idioma, tamaño y SHA-256, más la licencia que se le pase.

return args.FirstOrDefault() switch
{
    "usfx" when args.Length == 6 => Usfx(args[1], args[2], args[3], args[4], args[5]),
    "separar" when args.Length >= 3 => Separate(args[1], args[2..]),
    "lista" when args.Length >= 2 => List(args[1], args[2..]),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("Uso: usfx <zip|xml> <nombre> <sigla> <idioma> <salida.ecbiblias> | separar <carpeta> <paquetes…> | lista <carpeta> [SIGLA=licencia…]");
    return 2;
}

static int Usfx(string source, string name, string abbreviation, string language, string output)
{
    using var xml = OpenUsfx(source);
    var bible = UsfxReader.Read(xml);
    using (var file = File.Create(output))
        BiblePackage.Write(file, [(name, abbreviation, language, bible)]);
    Console.WriteLine($"{abbreviation}: {bible.Books.Count} libros, {bible.VerseCount:N0} versículos → {output}");
    return bible.Books.Count == 66 ? 0 : 1;
}

static Stream OpenUsfx(string source)
{
    if (!source.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        return File.OpenRead(source);

    using var zip = ZipFile.OpenRead(source);
    var entry = zip.Entries.First(e => e.Name.EndsWith("_usfx.xml", StringComparison.OrdinalIgnoreCase));
    var copy = new MemoryStream();
    using (var s = entry.Open())
        s.CopyTo(copy);
    copy.Position = 0;
    return copy;
}

static int Separate(string folder, string[] packages)
{
    Directory.CreateDirectory(folder);
    foreach (var path in packages)
    {
        IReadOnlyList<BiblePackageEntry> entries;
        using (var s = File.OpenRead(path))
            entries = BiblePackage.ReadManifest(s);

        foreach (var entry in entries)
        {
            ParsedBible bible;
            using (var s = File.OpenRead(path))
                bible = BiblePackage.ReadVersion(s, entry);
            var target = Path.Combine(folder, SafeName(entry.Abbreviation) + BiblePackage.Extension);
            using (var file = File.Create(target))
                BiblePackage.Write(file, [(entry.Name, entry.Abbreviation, entry.Language, bible)]);
            Console.WriteLine($"{entry.Abbreviation}: {bible.VerseCount:N0} versículos → {target}");
        }
    }

    return 0;
}

static int List(string folder, string[] licenses)
{
    var licenseOf = licenses
        .Select(l => l.Split('=', 2))
        .Where(p => p.Length == 2)
        .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

    var lines = new List<(BibleCatalogItem, string)>();
    foreach (var path in Directory.GetFiles(folder, "*" + BiblePackage.Extension).Order())
    {
        BiblePackageEntry entry;
        using (var s = File.OpenRead(path))
            entry = BiblePackage.ReadManifest(s).Single();

        var bytes = File.ReadAllBytes(path);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var fileName = Path.GetFileName(path);
        licenseOf.TryGetValue(entry.Abbreviation, out var license);
        lines.Add((new BibleCatalogItem(entry.Name, entry.Abbreviation, entry.Language,
            new Uri("https://x/" + fileName), bytes.Length, sha, license ?? string.Empty), fileName));
        Console.WriteLine($"{entry.Abbreviation,-8} {entry.Language} {bytes.Length,10:N0} {sha[..12]}… {license}");
    }

    File.WriteAllText(Path.Combine(folder, BibleCatalog.FileName), BibleCatalog.Write(lines), new UTF8Encoding(false));
    Console.WriteLine($"{lines.Count} Biblias en {Path.Combine(folder, BibleCatalog.FileName)}");
    return 0;
}

static string SafeName(string text) => Regex.Replace(text, @"[^\w\-]+", "_");

/// <summary>Lee USFX (el XML de eBible.org): libro, capítulo, versículo y el texto entre medio.</summary>
static class UsfxReader
{
    // Los 66 libros en el orden del programa (1 = Génesis … 66 = Apocalipsis).
    private static readonly string[] Codes =
    [
        "GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH",
        "EZR", "NEH", "EST", "JOB", "PSA", "PRO", "ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS",
        "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
        "MAT", "MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH",
        "1TI", "2TI", "TIT", "PHM", "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD", "REV",
    ];

    // Lo que no es texto de los versículos: notas, referencias, títulos, encabezados.
    private static readonly HashSet<string> Skipped =
        ["f", "x", "toc", "id", "ide", "h", "d", "s", "rem", "fig", "cl", "cp", "languageCode", "periph", "va", "vp", "ca"];

    public static ParsedBible Read(Stream xml)
    {
        var books = new List<ParsedBook>();
        ParsedBook? book = null;
        int chapter = 0, verse = 0, skipDepth = 0;
        var text = new StringBuilder();

        void Close()
        {
            if (book is not null && verse > 0)
            {
                // The KJV marks new paragraphs with «¶»: it is not part of the text.
                var clean = Regex.Replace(text.ToString().Replace("¶", " "), @"\s+", " ").Trim();
                // A note taken out of the middle leaves a space before the comma or the period.
                clean = Regex.Replace(clean, @"\s+([,.)])", "$1");
                if (clean.Length > 0)
                    book.Verses.Add(new ParsedVerse(chapter, verse, clean));
            }
            verse = 0;
            text.Clear();
        }

        using var reader = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                {
                    var name = reader.LocalName;
                    if (skipDepth > 0)
                    {
                        if (!reader.IsEmptyElement)
                            skipDepth++;
                        break;
                    }

                    if (name == "book")
                    {
                        Close();
                        var number = Array.IndexOf(Codes, reader.GetAttribute("id")) + 1;
                        book = number > 0
                            ? new ParsedBook(number, BibleBookCatalog.FindByNumber(number)?.Name ?? reader.GetAttribute("id")!, [])
                            : null;
                        if (book is not null)
                            books.Add(book);
                        chapter = 0;
                    }
                    else if (name == "c")
                    {
                        Close();
                        chapter = int.TryParse(reader.GetAttribute("id"), out var c) ? c : chapter;
                    }
                    else if (name == "v")
                    {
                        Close();
                        verse = int.TryParse(Regex.Match(reader.GetAttribute("id") ?? "", @"^\d+").Value, out var v) ? v : 0;
                    }
                    else if (name == "ve")
                    {
                        Close();
                    }
                    else if (Skipped.Contains(name) && !reader.IsEmptyElement)
                    {
                        skipDepth = 1;
                    }
                    break;
                }
                case XmlNodeType.EndElement:
                    if (skipDepth > 0)
                        skipDepth--;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    if (skipDepth == 0 && verse > 0)
                        text.Append(reader.Value);
                    break;
            }
        }

        Close();
        books.RemoveAll(b => b.Verses.Count == 0);
        return new ParsedBible(books);
    }
}
