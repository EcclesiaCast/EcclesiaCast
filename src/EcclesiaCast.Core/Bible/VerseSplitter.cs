namespace EcclesiaCast.Core.Bible;

/// <summary>
/// Cuts a long verse over several slides when the Bible theme asks for at most
/// N characters on each. It uses as few slides as that limit allows and makes
/// them about the same length, cutting after a period or a semicolon when one
/// is near, else after a comma or a colon, else between two words. A word is
/// never broken: one longer than the limit stays whole on its slide.
/// </summary>
public static class VerseSplitter
{
    /// <summary>The limit proposed when the option is turned on.</summary>
    public const int DefaultMaxChars = 150;

    public const int MinMaxChars = 50;
    public const int MaxMaxChars = 500;

    // What a cut costs on top of how uneven it leaves the slides. The unevenness
    // is the square of how far each part is from the average, as a fraction of
    // it, so with two parts a period wins over a comma in the middle while it is
    // less than about 30 % off, and a comma wins over a bare space about as far.
    private const double CommaCost = 0.2;
    private const double WordCost = 0.45;

    private readonly record struct Word(int Start, int End, double CutCost);

    /// <summary>
    /// The verse in the parts it takes, in order; one part, untouched, when it
    /// fits or when <paramref name="maxChars"/> is 0 (the option is off). With
    /// two versions both are cut in the same number of parts, the one the longer
    /// of them needs, so they keep walking together. A version with fewer words
    /// than parts leaves the last ones without text (empty main, null second).
    /// </summary>
    public static IReadOnlyList<(string Main, string? Secondary)> Split(string main, string? secondary, int maxChars)
    {
        if (maxChars <= 0)
            return [(main, secondary)];

        var mainWords = WordsOf(main);
        var secondWords = secondary is null ? [] : WordsOf(secondary);
        var count = Math.Max(PartsNeeded(mainWords, maxChars), PartsNeeded(secondWords, maxChars));
        if (count <= 1)
            return [(main, secondary)];

        var mains = Cut(main, mainWords, count, maxChars);
        var seconds = secondary is null ? null : Cut(secondary, secondWords, count, maxChars);

        return Enumerable.Range(0, count)
            .Select(i => (
                i < mains.Count ? mains[i] : string.Empty,
                seconds is not null && i < seconds.Count ? seconds[i] : null))
            .ToList();
    }

    /// <summary>The fewest parts of at most <paramref name="maxChars"/> (or of a single word) the words go in.</summary>
    private static int PartsNeeded(List<Word> words, int maxChars)
    {
        if (words.Count == 0)
            return 1;

        var parts = 1;
        var start = words[0].Start;
        for (var i = 1; i < words.Count; i++)
        {
            if (words[i].End - start <= maxChars)
                continue;
            parts++;
            start = words[i].Start;
        }

        return parts;
    }

    /// <summary>
    /// The best way of cutting the words in exactly <paramref name="count"/>
    /// parts (or one per word, when there are fewer), looking at all of them at
    /// once so no part is left with the leftovers of the others.
    /// </summary>
    private static List<string> Cut(string text, List<Word> words, int count, int maxChars)
    {
        count = Math.Min(count, words.Count);
        if (count == 0)
            return [];

        var average = Math.Max(1.0, (words[^1].End - words[0].Start - (count - 1)) / (double)count);

        // best[k, n]: the lowest cost of putting the first n words in k parts;
        // from[k, n]: the word where the last of those parts starts.
        var best = new double[count + 1, words.Count + 1];
        var from = new int[count + 1, words.Count + 1];
        for (var k = 0; k <= count; k++)
            for (var n = 0; n <= words.Count; n++)
                best[k, n] = double.PositiveInfinity;
        best[0, 0] = 0;

        for (var k = 1; k <= count; k++)
        {
            for (var end = k; end <= words.Count; end++)
            {
                var cutCost = end < words.Count ? words[end - 1].CutCost : 0;
                for (var start = end - 1; start >= k - 1; start--)
                {
                    var length = words[end - 1].End - words[start].Start;
                    if (length > maxChars && end - start > 1)
                        break;
                    if (double.IsPositiveInfinity(best[k - 1, start]))
                        continue;

                    var off = (length - average) / average;
                    var cost = best[k - 1, start] + off * off + cutCost;
                    if (cost < best[k, end])
                    {
                        best[k, end] = cost;
                        from[k, end] = start;
                    }
                }
            }
        }

        var parts = new string[count];
        for (int k = count, end = words.Count; k > 0; k--)
        {
            var start = from[k, end];
            parts[k - 1] = text[words[start].Start..words[end - 1].End];
            end = start;
        }

        return [.. parts];
    }

    private static List<Word> WordsOf(string text)
    {
        var words = new List<Word>();
        var i = 0;
        while (i < text.Length)
        {
            if (IsSpace(text[i]))
            {
                i++;
                continue;
            }

            var start = i;
            while (i < text.Length && !IsSpace(text[i]))
                i++;

            // A mark standing alone (a dash, a closing quote) stays with the
            // word before it, so no part starts with it.
            if (words.Count > 0 && ClosesWhatCameBefore(text.AsSpan(start, i - start)))
                start = words[^1].Start;
            else
                words.Add(default);

            words[^1] = new Word(start, i, CostOfCuttingAfter(text.AsSpan(start, i - start)));
        }

        return words;
    }

    /// <summary>A non-breaking space is there so the line is not cut at it.</summary>
    private static bool IsSpace(char c) => char.IsWhiteSpace(c) && c != ' ';

    private static bool ClosesWhatCameBefore(ReadOnlySpan<char> token)
    {
        foreach (var c in token)
        {
            if (char.IsLetterOrDigit(c) || c is '«' or '“' or '‘' or '(' or '[' or '¿' or '¡')
                return false;
        }

        return true;
    }

    private static double CostOfCuttingAfter(ReadOnlySpan<char> word)
    {
        // The mark that counts may sit before a closing quote: «…vida.»
        var last = word.Length - 1;
        while (last >= 0 && (word[last] is '»' or '"' or '”' or '’' or '\'' or ')' or ']' || IsSpace(word[last])))
            last--;

        return last < 0 ? WordCost : word[last] switch
        {
            '.' or ';' or '?' or '!' or '…' => 0,
            ',' or ':' => CommaCost,
            _ => WordCost,
        };
    }
}
