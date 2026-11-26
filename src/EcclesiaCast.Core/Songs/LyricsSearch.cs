using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// «Buscar en la letra»: which slides of the open song have the words typed,
/// for when the singer jumps to another part and the operator has to find it
/// fast. Accents, capitals, punctuation and line breaks do not count, so «senor»
/// finds «Señor,» even when the phrase runs over two lines.
/// </summary>
public static class LyricsSearch
{
    /// <summary>The positions (in order) of the texts that contain <paramref name="query"/>.</summary>
    public static IReadOnlyList<int> Matches(IReadOnlyList<string> texts, string? query)
    {
        var wanted = Normalize(query);
        if (wanted.Length == 0)
            return [];

        var found = new List<int>();
        for (var i = 0; i < texts.Count; i++)
            if (Normalize(texts[i]).Contains(wanted, StringComparison.Ordinal))
                found.Add(i);
        return found;
    }

    /// <summary>True when the words typed appear in the text starting at the beginning of a word.</summary>
    public static bool StartsAtWord(string? text, string normalizedQuery) =>
        normalizedQuery.Length > 0
        && string.Concat(" ", Normalize(text)).Contains(string.Concat(" ", normalizedQuery), StringComparison.Ordinal);

    /// <summary>
    /// Every character in lowercase and without its accent, worked out once: a
    /// search goes through whole Bibles and can't take each text apart.
    /// </summary>
    private static readonly char[] Folded = BuildFolded();

    private static char[] BuildFolded()
    {
        var table = new char[char.MaxValue + 1];
        for (var i = 0; i <= char.MaxValue; i++)
        {
            var c = char.ToLowerInvariant((char)i);
            if (c >= 0x80 && c < 0x2000 && !char.IsSurrogate(c))
                c = c.ToString().Normalize(NormalizationForm.FormD)[0];
            table[i] = c;
        }

        return table;
    }

    /// <summary>Lowercase, without accents or punctuation, one space between words.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var raw in text)
        {
            // A letter typed as letter + loose accent: the accent is skipped.
            if (CharUnicodeInfo.GetUnicodeCategory(raw) == UnicodeCategory.NonSpacingMark)
                continue;

            var c = Folded[raw];
            if (char.IsLetterOrDigit(c))
            {
                if (space && builder.Length > 0)
                    builder.Append(' ');
                builder.Append(c);
                space = false;
            }
            else
            {
                space = true;
            }
        }

        return builder.ToString();
    }
}
