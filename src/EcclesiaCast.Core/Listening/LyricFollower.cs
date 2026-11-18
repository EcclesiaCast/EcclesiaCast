namespace EcclesiaCast.Core.Listening;

/// <summary>
/// Where the singing is, as far as the last words heard tell: which slide, how
/// far into it, and how sure.
/// </summary>
/// <param name="Slide">The slide the words belong to.</param>
/// <param name="WordsLeft">Words of that slide still to be sung after the last one recognised.</param>
/// <param name="Matched">How many of the words heard were found, in order, in the song.</param>
/// <param name="Confidence">0 to 1: the share of the recent words that fit, in order.</param>
/// <param name="Score">How much evidence there is: the matched words, the short ones counting less.</param>
/// <param name="InSlide">How many of the matched words are in <paramref name="Slide"/> itself.</param>
public readonly record struct LyricPosition(
    int Slide, int WordsLeft, int Matched, double Confidence, double Score = 0, int InSlide = 0);

/// <summary>
/// Finds the place of a known song that a run of recognised words belongs to.
/// The words are compared with the whole song as it is sung, slide after
/// slide, so a line that runs from the end of one slide into the next one
/// fits as well as a line inside one. The recogniser makes mistakes and drops
/// words over a band; this does not need a transcript, only enough words in
/// order.
/// </summary>
public sealed class LyricFollower
{
    /// <summary>How many of the latest words <see cref="Hear"/> keeps: about one sung line.</summary>
    public const int Window = 7;

    /// <summary>How many words a whole window of sound may bring (<see cref="HearAll"/>).</summary>
    public const int LongWindow = 24;

    /// <summary>Fewer words than this say nothing yet.</summary>
    public const int MinimumHeard = 3;

    /// <summary>Lost per word skipped between two that matched, on either side.</summary>
    private const double GapPenalty = 0.12;

    /// <summary>Words that may be skipped between two matches.</summary>
    private const int MaxGap = 3;

    /// <summary>A place must end on one of this many latest words: where the singing is, not where it was.</summary>
    private const int Recent = 3;

    private readonly string[] _words;
    private readonly int[] _slideOf;
    private readonly double[] _weight;
    private readonly int[] _slideEnd;
    private readonly List<string> _heard = [];

    public LyricFollower(IEnumerable<string> slideTexts)
    {
        var words = new List<string>();
        var slideOf = new List<int>();
        var ends = new List<int>();
        var s = 0;
        foreach (var text in slideTexts)
        {
            foreach (var word in LyricSounds.Words(text))
            {
                words.Add(word);
                slideOf.Add(s);
            }

            ends.Add(words.Count - 1);
            s++;
        }

        _words = [.. words];
        _slideOf = [.. slideOf];
        _slideEnd = [.. ends];
        _weight = [.. _words.Select(Weight)];
    }

    public int SlideCount => _slideEnd.Length;

    /// <summary>The place of a word in the whole song, counting from the first word of the first slide.</summary>
    public int WordIndex(LyricPosition position) => _slideEnd[position.Slide] - position.WordsLeft;

    /// <summary>The place in the whole song of the first word of a slide.</summary>
    public int FirstWordOf(int slide) => slide <= 0 ? 0 : _slideEnd[Math.Min(slide, _slideEnd.Length) - 1] + 1;

    /// <summary>Adds what the recogniser just heard, in order, keeping about a line.</summary>
    public void Hear(string? text)
    {
        _heard.AddRange(LyricSounds.Words(text));
        if (_heard.Count > Window)
            _heard.RemoveRange(0, _heard.Count - Window);
    }

    /// <summary>Takes everything heard in a stretch of sound, in place of what was heard before.</summary>
    public void HearAll(string? text)
    {
        _heard.Clear();
        _heard.AddRange(LyricSounds.Words(text));
        if (_heard.Count > LongWindow)
            _heard.RemoveRange(0, _heard.Count - LongWindow);
    }

    /// <summary>Forgets what was heard: after a change of slide, old words must not count again.</summary>
    public void Forget() => _heard.Clear();

    /// <summary>
    /// The best place for the latest words, or null when they are too few or
    /// fit nowhere. Among places that fit the same (a chorus that comes
    /// twice), the slide on screen wins, then the one after it, then the
    /// nearest ahead.
    /// </summary>
    public LyricPosition? Locate(int liveSlide)
    {
        LyricPosition? best = null;
        var bestRank = double.MinValue;
        foreach (var position in Candidates())
        {
            var rank = position.Confidence + Nearness(position.Slide, liveSlide);
            if (rank > bestRank)
            {
                bestRank = rank;
                best = position;
            }
        }

        return best;
    }

    /// <summary>
    /// Every slide the latest words could be in, each with its best fit. A
    /// chorus sung twice in the song gives one candidate for each time.
    /// </summary>
    public IReadOnlyList<LyricPosition> Candidates()
    {
        var m = _heard.Count;
        var n = _words.Length;
        var found = new List<LyricPosition>();
        if (m < MinimumHeard || n == 0)
            return found;

        var score = new double[m, n];
        var count = new int[m, n];
        var first = new int[m, n]; // the first heard word of the run, for the confidence
        var same = new int[m, n];  // matches inside the slide of j
        var bestAt = new (double Score, int Count, int First, int Same)[n];
        var heardWeight = new double[m + 1];
        for (var i = 0; i < m; i++)
            heardWeight[i + 1] = heardWeight[i] + Weight(_heard[i]);

        for (var i = 0; i < m; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var alike = LyricSounds.Similarity(_heard[i], _words[j]);
                if (alike <= 0)
                    continue;

                double carried = 0;
                var carriedCount = 0;
                var carriedSame = 0;
                var start = i;
                for (var pi = Math.Max(0, i - MaxGap - 1); pi < i; pi++)
                {
                    for (var pj = Math.Max(0, j - MaxGap - 1); pj < j; pj++)
                    {
                        if (count[pi, pj] == 0)
                            continue;
                        var value = score[pi, pj] - GapPenalty * ((i - pi - 1) + (j - pj - 1));
                        if (value > carried)
                        {
                            carried = value;
                            carriedCount = count[pi, pj];
                            carriedSame = _slideOf[pj] == _slideOf[j] ? same[pi, pj] : 0;
                            start = first[pi, pj];
                        }
                    }
                }

                score[i, j] = alike * _weight[j] + carried;
                count[i, j] = carriedCount + 1;
                first[i, j] = start;
                same[i, j] = carriedSame + 1;

                if (i >= m - Recent && score[i, j] > bestAt[j].Score)
                    bestAt[j] = (score[i, j], count[i, j], start, same[i, j]);
            }
        }

        // One candidate per slide: the run ending furthest into it among the best.
        for (var s = 0; s < _slideEnd.Length; s++)
        {
            var from = s == 0 ? 0 : _slideEnd[s - 1] + 1;
            (double Score, int Count, int First, int Same, int End) best = default;
            for (var j = from; j <= _slideEnd[s]; j++)
            {
                if (bestAt[j].Count == 0)
                    continue;
                if (best.Count == 0 || bestAt[j].Score > best.Score + 1e-9
                    || (Math.Abs(bestAt[j].Score - best.Score) < 1e-9 && j > best.End))
                    best = (bestAt[j].Score, bestAt[j].Count, bestAt[j].First, bestAt[j].Same, j);
            }

            if (best.Count == 0)
                continue;

            // The share of the heard words from where the run starts to now that fit.
            var span = heardWeight[m] - heardWeight[best.First];
            found.Add(new LyricPosition(s, _slideEnd[s] - best.End, best.Count,
                Math.Min(1, best.Score / Math.Max(span, 2.5)), best.Score, best.Same));
        }

        return found;
    }

    /// <summary>A small push for the slides the song is most likely on, to break ties.</summary>
    private static double Nearness(int slide, int live)
    {
        if (live < 0)
            return 0;
        if (slide == live)
            return 0.05;
        if (slide == live + 1)
            return 0.04;
        return slide > live ? 0.01 / (slide - live) : 0;
    }

    /// <summary>
    /// What a word is worth as evidence: «de», «la», «y» are in every line,
    /// «despertando» is not.
    /// </summary>
    private static double Weight(string sound) => sound.Length switch
    {
        <= 2 => 0.5,
        3 => 0.75,
        _ => 1,
    };
}
