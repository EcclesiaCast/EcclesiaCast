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

    /// <summary>Lowercase, without accents or punctuation, one space between words.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

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
