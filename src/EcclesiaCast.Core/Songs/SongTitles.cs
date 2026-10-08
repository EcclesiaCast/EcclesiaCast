using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>A song's id and title, enough to tell whether a title is taken.</summary>
public sealed record SongTitle(int Id, string Title);

/// <summary>
/// When two songs count as the same one, and what to call a new song whose
/// title is already in the library. Titles are compared without telling
/// capitals or accents apart («Mi Gozo» is «mi gozó»): the same song arrives
/// from different sources with different typing.
/// </summary>
public static partial class SongTitles
{
    /// <summary>The title as it is compared: no accents, lower case, single spaces.</summary>
    public static string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var decomposed = title.Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                plain.Append(c);
        }

        return Spaces().Replace(plain.ToString().Normalize(NormalizationForm.FormC), " ").Trim().ToLowerInvariant();
    }

    public static bool Same(string? a, string? b) =>
        Normalize(a) is { Length: > 0 } left && left == Normalize(b);

    /// <summary>The first song whose title is the same as <paramref name="title"/>, or null.</summary>
    public static SongTitle? FindSame(IEnumerable<SongTitle> library, string? title)
    {
        var wanted = Normalize(title);
        return wanted.Length == 0 ? null : library.FirstOrDefault(s => Normalize(s.Title) == wanted);
    }

    /// <summary>
    /// «Title (1)»: the title with the first number that is not taken yet. A
    /// title that already ends in a number in brackets counts from its own
    /// base, so «Mi Gozo (1)» becomes «Mi Gozo (2)», not «Mi Gozo (1) (1)».
    /// </summary>
    public static string FreeTitle(IEnumerable<string> existing, string title)
    {
        var taken = existing.Select(Normalize).ToHashSet();
        var baseTitle = TrailingNumber().Replace(title.Trim(), string.Empty).TrimEnd();
        if (baseTitle.Length == 0)
            baseTitle = title.Trim();

        for (var n = 1; ; n++)
        {
            var candidate = $"{baseTitle} ({n})";
            if (!taken.Contains(Normalize(candidate)))
                return candidate;
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\s*\(\d+\)\s*$")]
    private static partial Regex TrailingNumber();
}
