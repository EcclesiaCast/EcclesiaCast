using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>A song's id, title and artist, enough to tell whether a song is already in the library.</summary>
public sealed record SongTitle(int Id, string Title, string Artist = "");

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

    /// <summary>
    /// Title and artist together are the song: «Hay Libertad» by one band and
    /// «Hay Libertad» by another are two songs. When one of the two has no
    /// artist written there is no way to tell them apart, so it counts as the
    /// same song and the user is asked.
    /// </summary>
    public static bool SameSong(string? title, string? artist, string? otherTitle, string? otherArtist)
    {
        if (!Same(title, otherTitle))
            return false;

        var a = Normalize(artist);
        var b = Normalize(otherArtist);
        return a.Length == 0 || b.Length == 0 || a == b;
    }

    /// <summary>The first song that is the same as <paramref name="title"/> by <paramref name="artist"/>, or null.</summary>
    public static SongTitle? FindSame(IEnumerable<SongTitle> library, string? title, string? artist = null) =>
        Normalize(title).Length == 0
            ? null
            : library.FirstOrDefault(s => SameSong(s.Title, s.Artist, title, artist));

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
