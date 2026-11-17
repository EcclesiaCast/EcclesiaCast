namespace EcclesiaCast.Core.Bible;

/// <summary>The groups the book grid colours by (Ley, Históricos, Poéticos…).</summary>
public enum BibleBookGroup
{
    Law,
    History,
    Poetry,
    MajorProphets,
    MinorProphets,
    Gospels,
    Acts,
    PaulLetters,
    GeneralLetters,
    Revelation,
}

/// <summary>Group, short name and colour of each book, for the grid view of the Bible.</summary>
public static class BibleBookGroups
{
    public static BibleBookGroup GroupOf(int bookNumber) => bookNumber switch
    {
        <= 5 => BibleBookGroup.Law,
        <= 17 => BibleBookGroup.History,
        <= 22 => BibleBookGroup.Poetry,
        <= 27 => BibleBookGroup.MajorProphets,
        <= 39 => BibleBookGroup.MinorProphets,
        <= 43 => BibleBookGroup.Gospels,
        44 => BibleBookGroup.Acts,
        <= 57 => BibleBookGroup.PaulLetters,
        <= 65 => BibleBookGroup.GeneralLetters,
        _ => BibleBookGroup.Revelation,
    };

    public static string GroupName(BibleBookGroup group) => group switch
    {
        BibleBookGroup.Law => "Ley",
        BibleBookGroup.History => "Históricos",
        BibleBookGroup.Poetry => "Poéticos",
        BibleBookGroup.MajorProphets => "Profetas mayores",
        BibleBookGroup.MinorProphets => "Profetas menores",
        BibleBookGroup.Gospels => "Evangelios",
        BibleBookGroup.Acts => "Hechos",
        BibleBookGroup.PaulLetters => "Cartas de Pablo",
        BibleBookGroup.GeneralLetters => "Cartas generales",
        _ => "Apocalipsis",
    };

    /// <summary>
    /// Colour of the group (#RRGGBB): the tile's border and abbreviation. Soft
    /// enough to sit on the dark panel; the tile's fill is this colour at low
    /// opacity.
    /// </summary>
    public static string ColorHex(BibleBookGroup group) => group switch
    {
        BibleBookGroup.Law => "#D9A441",
        BibleBookGroup.History => "#C9743C",
        BibleBookGroup.Poetry => "#B86AB5",
        BibleBookGroup.MajorProphets => "#D2605F",
        BibleBookGroup.MinorProphets => "#8E7BD6",
        BibleBookGroup.Gospels => "#4F8FE0",
        BibleBookGroup.Acts => "#3FB2B8",
        BibleBookGroup.PaulLetters => "#58B368",
        BibleBookGroup.GeneralLetters => "#9DB84A",
        _ => "#E08A9E",
    };

    /// <summary>«Gn», «Ex», «Jn», «1Co»: the first short name of the book, with the capitals a person writes.</summary>
    public static string Abbreviation(BibleBookInfo book)
    {
        var raw = book.Aliases.Length > 0 ? book.Aliases[0] : book.Name;
        var chars = raw.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsLetter(chars[i]))
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                break;
            }
        }

        return new string(chars);
    }

    /// <summary>The groups in order, with the books each has among those given (empty ones left out).</summary>
    public static IReadOnlyList<(BibleBookGroup Group, IReadOnlyList<BibleBookInfo> Books)> Arrange(IEnumerable<BibleBookInfo> books) =>
        books.GroupBy(b => GroupOf(b.Number))
             .OrderBy(g => g.Key)
             .Select(g => (g.Key, (IReadOnlyList<BibleBookInfo>)g.OrderBy(b => b.Number).ToList()))
             .ToList();
}
