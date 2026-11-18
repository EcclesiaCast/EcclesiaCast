using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Listening;

/// <summary>
/// Turns words into how they sound in Spanish, so what a speech recogniser
/// heard can be compared with the lyrics as written: «vendita» and «bendita»,
/// «hasia» and «hacia», «yeno» and «lleno» are the same word to the ear. It is
/// deliberately rough: the point is to recognise a line of a known song among
/// a handful, not to spell.
/// </summary>
public static class LyricSounds
{
    /// <summary>The words of a text, each as its sound; punctuation and empty words are dropped.</summary>
    public static IReadOnlyList<string> Words(string? text)
    {
        var words = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return words;

        var current = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(ch);
                continue;
            }

            Flush();
        }

        Flush();
        return words;

        void Flush()
        {
            if (current.Length == 0)
                return;
            var key = Key(current.ToString());
            current.Clear();
            if (key.Length > 0)
                words.Add(key);
        }
    }

    /// <summary>One word as its sound: no accents, no silent letters, one letter per sound.</summary>
    public static string Key(string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var plain = Plain(word);
        var key = new StringBuilder(plain.Length);

        for (var i = 0; i < plain.Length; i++)
        {
            var c = plain[i];
            var next = i + 1 < plain.Length ? plain[i + 1] : '\0';
            var soft = next is 'e' or 'i';

            switch (c)
            {
                case 'h':
                    break; // silent; «ch» was taken with its «c»
                case 'c' when next == 'h':
                    Add('x');
                    i++;
                    break;
                case 'c':
                    Add(soft ? 's' : 'k');
                    break;
                case 'q':
                    Add('k');
                    if (next == 'u')
                        i++;
                    break;
                case 'g' when next == 'u' && i + 2 < plain.Length && plain[i + 2] is 'e' or 'i':
                    Add('g');
                    i++;
                    break;
                case 'g':
                    Add(soft ? 'j' : 'g');
                    break;
                case 'l' when next == 'l':
                    Add('y');
                    i++;
                    break;
                case 'v':
                    Add('b');
                    break;
                case 'z' or 'x':
                    Add('s');
                    break;
                case 'w':
                    Add('u');
                    break;
                case 'y' when next == '\0' || !IsVowel(next):
                    Add('i'); // «y», «rey»
                    break;
                default:
                    Add(c);
                    break;
            }
        }

        return key.ToString();

        // The same sound twice in a row is one («ss», «rr», «cc»).
        void Add(char sound)
        {
            if (key.Length == 0 || key[^1] != sound)
                key.Append(sound);
        }
    }

    /// <summary>
    /// How alike two sounds are, 0 to 1: the same, or one letter apart in a
    /// word long enough for that to mean «the same word, heard badly».
    /// </summary>
    public static double Similarity(string a, string b)
    {
        if (a == b)
            return 1;
        if (a.Length < 5 || b.Length < 5 || Math.Abs(a.Length - b.Length) > 1)
            return 0;
        return WithinOneEdit(a, b) ? 0.7 : 0;
    }

    private static bool WithinOneEdit(string a, string b)
    {
        if (a.Length > b.Length)
            (a, b) = (b, a);

        int i = 0, j = 0;
        var edits = 0;
        while (i < a.Length && j < b.Length)
        {
            if (a[i] == b[j])
            {
                i++;
                j++;
                continue;
            }

            if (++edits > 1)
                return false;
            if (a.Length == b.Length)
                i++;
            j++;
        }

        return edits + (b.Length - j) <= 1;
    }

    private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u';

    /// <summary>Lower case, without accents; «ñ» stays apart from «n» only as far as «ni» does.</summary>
    private static string Plain(string word)
    {
        var decomposed = word.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                plain.Append(ch);
        }

        return plain.ToString();
    }
}
