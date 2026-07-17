using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Bible;

/// <summary>The canonical 66 books, used to resolve references typed by the operator.</summary>
public static class BibleBookCatalog
{
    public static IReadOnlyList<BibleBookInfo> Books { get; } =
    [
        // ── Antiguo Testamento ──────────────────────────────────
        new(1, "Génesis", Testament.Old, ["gn", "gen", "genesis"]),
        new(2, "Éxodo", Testament.Old, ["ex", "exo", "exodo"]),
        new(3, "Levítico", Testament.Old, ["lv", "lev", "levitico"]),
        new(4, "Números", Testament.Old, ["nm", "num", "numeros"]),
        new(5, "Deuteronomio", Testament.Old, ["dt", "deut", "deuteronomio"]),
        new(6, "Josué", Testament.Old, ["jos", "josue"]),
        new(7, "Jueces", Testament.Old, ["jue", "jueces"]),
        new(8, "Rut", Testament.Old, ["rt", "rut"]),
        new(9, "1 Samuel", Testament.Old, ["1s", "1sa", "1sam", "1samuel"]),
        new(10, "2 Samuel", Testament.Old, ["2s", "2sa", "2sam", "2samuel"]),
        new(11, "1 Reyes", Testament.Old, ["1r", "1re", "1rey", "1reyes"]),
        new(12, "2 Reyes", Testament.Old, ["2r", "2re", "2rey", "2reyes"]),
        new(13, "1 Crónicas", Testament.Old, ["1cr", "1cro", "1cronicas"]),
        new(14, "2 Crónicas", Testament.Old, ["2cr", "2cro", "2cronicas"]),
        new(15, "Esdras", Testament.Old, ["esd", "esdras"]),
        new(16, "Nehemías", Testament.Old, ["neh", "nehemias"]),
        new(17, "Ester", Testament.Old, ["est", "ester"]),
        new(18, "Job", Testament.Old, ["job"]),
        new(19, "Salmos", Testament.Old, ["sal", "salmo", "salmos", "ps"]),
        new(20, "Proverbios", Testament.Old, ["pr", "prov", "proverbios"]),
        new(21, "Eclesiastés", Testament.Old, ["ec", "ecl", "eclesiastes"]),
        new(22, "Cantares", Testament.Old, ["cnt", "cant", "cantares", "cantardeloscantares"]),
        new(23, "Isaías", Testament.Old, ["is", "isa", "isaias"]),
        new(24, "Jeremías", Testament.Old, ["jer", "jeremias"]),
        new(25, "Lamentaciones", Testament.Old, ["lm", "lam", "lamentaciones"]),
        new(26, "Ezequiel", Testament.Old, ["ez", "eze", "ezequiel"]),
        new(27, "Daniel", Testament.Old, ["dn", "dan", "daniel"]),
        new(28, "Oseas", Testament.Old, ["os", "ose", "oseas"]),
        new(29, "Joel", Testament.Old, ["jl", "joel"]),
        new(30, "Amós", Testament.Old, ["am", "amos"]),
        new(31, "Abdías", Testament.Old, ["abd", "abdias"]),
        new(32, "Jonás", Testament.Old, ["jon", "jonas"]),
        new(33, "Miqueas", Testament.Old, ["mi", "miq", "miqueas"]),
        new(34, "Nahúm", Testament.Old, ["na", "nah", "nahum"]),
        new(35, "Habacuc", Testament.Old, ["hab", "habacuc"]),
        new(36, "Sofonías", Testament.Old, ["sof", "sofonias"]),
        new(37, "Hageo", Testament.Old, ["hag", "hageo"]),
        new(38, "Zacarías", Testament.Old, ["zac", "zacarias"]),
        new(39, "Malaquías", Testament.Old, ["mal", "malaquias"]),

        // ── Nuevo Testamento ────────────────────────────────────
        new(40, "Mateo", Testament.New, ["mt", "mat", "mateo"]),
        new(41, "Marcos", Testament.New, ["mc", "mr", "mar", "marcos"]),
        new(42, "Lucas", Testament.New, ["lc", "luc", "lucas"]),
        new(43, "Juan", Testament.New, ["jn", "juan"]),
        new(44, "Hechos", Testament.New, ["hch", "hech", "hechos"]),
        new(45, "Romanos", Testament.New, ["ro", "rom", "romanos"]),
        new(46, "1 Corintios", Testament.New, ["1co", "1cor", "1corintios"]),
        new(47, "2 Corintios", Testament.New, ["2co", "2cor", "2corintios"]),
        new(48, "Gálatas", Testament.New, ["ga", "gal", "galatas"]),
        new(49, "Efesios", Testament.New, ["ef", "efe", "efesios"]),
        new(50, "Filipenses", Testament.New, ["fil", "flp", "filipenses"]),
        new(51, "Colosenses", Testament.New, ["col", "colosenses"]),
        new(52, "1 Tesalonicenses", Testament.New, ["1ts", "1tes", "1tesalonicenses"]),
        new(53, "2 Tesalonicenses", Testament.New, ["2ts", "2tes", "2tesalonicenses"]),
        new(54, "1 Timoteo", Testament.New, ["1ti", "1tim", "1timoteo"]),
        new(55, "2 Timoteo", Testament.New, ["2ti", "2tim", "2timoteo"]),
        new(56, "Tito", Testament.New, ["tit", "tito"]),
        new(57, "Filemón", Testament.New, ["flm", "filemon"]),
        new(58, "Hebreos", Testament.New, ["heb", "hebreos"]),
        new(59, "Santiago", Testament.New, ["stg", "sant", "santiago"]),
        new(60, "1 Pedro", Testament.New, ["1p", "1pe", "1pedro"]),
        new(61, "2 Pedro", Testament.New, ["2p", "2pe", "2pedro"]),
        new(62, "1 Juan", Testament.New, ["1jn", "1j", "1juan"]),
        new(63, "2 Juan", Testament.New, ["2jn", "2j", "2juan"]),
        new(64, "3 Juan", Testament.New, ["3jn", "3j", "3juan"]),
        new(65, "Judas", Testament.New, ["jud", "judas"]),
        new(66, "Apocalipsis", Testament.New, ["ap", "apoc", "apocalipsis"]),
    ];

    public static BibleBookInfo? FindByNumber(int number) =>
        Books.FirstOrDefault(b => b.Number == number);

    /// <summary>
    /// Matches free-typed book text (full name or abbreviation, any casing,
    /// with or without accents/spaces) against the catalog.
    /// </summary>
    public static BibleBookInfo? FindByName(string text)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
            return null;

        return Books.FirstOrDefault(b =>
            Normalize(b.Name) == normalized
            || b.Aliases.Any(a => Normalize(a) == normalized));
    }

    /// <summary>Lowercase, accent-free, space-free — so "1 Co", "1co" and "1 CORINTIOS" all compare equal.</summary>
    internal static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (c is ' ' or '.')
                continue;
            builder.Append(c);
        }

        return builder.ToString();
    }
}
