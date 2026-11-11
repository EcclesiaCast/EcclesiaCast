using System.Text;

namespace EcclesiaCast.Core.Listening;

/// <summary>
/// Tidies what the speech recogniser hands back before it is compared with
/// the lyrics, and throws away what it made up. Over music a recogniser fills
/// silence with stock phrases («Música», «Suscríbete al canal», «Gracias por
/// ver el video») or repeats one line many times: none of that was sung.
/// </summary>
public static class HeardText
{
    /// <summary>Nobody sings faster than this for long; more words per second were invented.</summary>
    public const double MaxWordsPerSecond = 4.5;

    private static readonly string[] StockPhrases =
    [
        "suscribete", "subtitulos", "amara org", "gracias por ver", "musica",
    ];

    /// <summary>
    /// The words that were plausibly sung in <paramref name="seconds"/> of
    /// audio, or an empty text when the recogniser was filling in.
    /// </summary>
    public static string Clean(string? raw, double seconds)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = WithoutAsides(raw);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return string.Empty;

        if (seconds > 0 && words.Length / seconds > MaxWordsPerSecond)
            return string.Empty;

        var plain = string.Join(' ', LyricSounds.Words(text));
        var folded = Fold(text);
        foreach (var phrase in StockPhrases)
        {
            // «Música» alone is a label; inside a sung line it is a word.
            if (folded.Contains(phrase, StringComparison.Ordinal) && words.Length <= 4)
                return string.Empty;
        }

        return IsOneLineRepeated(plain) ? string.Empty : text;
    }

    /// <summary>Drops what the recogniser writes between brackets, parentheses or notes: [Música], (aplausos), ♪.</summary>
    private static string WithoutAsides(string raw)
    {
        var kept = new StringBuilder(raw.Length);
        var depth = 0;
        foreach (var ch in raw)
        {
            switch (ch)
            {
                case '[' or '(':
                    depth++;
                    break;
                case ']' or ')':
                    depth = Math.Max(0, depth - 1);
                    break;
                case '♪' or '♫' or '¶':
                    kept.Append(' ');
                    break;
                default:
                    if (depth == 0)
                        kept.Append(ch);
                    break;
            }
        }

        return string.Join(' ', kept.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>The same few words three times or more in a row and nothing else: a loop, not a song.</summary>
    private static bool IsOneLineRepeated(string soundWords)
    {
        var words = soundWords.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var size = 1; size <= words.Length / 3; size++)
        {
            if (words.Length % size != 0)
                continue;

            var repeats = true;
            for (var i = size; i < words.Length && repeats; i++)
                repeats = words[i] == words[i % size];

            if (repeats)
                return true;
        }

        return false;
    }

    private static string Fold(string text)
    {
        var folded = new StringBuilder(text.Length);
        foreach (var ch in text.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetter(ch) || ch == ' ')
                folded.Append(ch);
            else if (ch == '.')
                folded.Append(' ');
        }

        return folded.ToString();
    }
}
