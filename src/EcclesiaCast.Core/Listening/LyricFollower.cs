namespace EcclesiaCast.Core.Listening;

/// <summary>
/// Where the singing is, as far as the last words heard tell: which slide, how
/// far into it, and how sure.
/// </summary>
/// <param name="Slide">The slide the words belong to.</param>
/// <param name="WordsLeft">Words of that slide still to be sung after the last one recognised.</param>
/// <param name="Matched">How many of the words heard were found in the slide.</param>
/// <param name="Confidence">0 to 1: the share of the recent words that fit, in order.</param>
public readonly record struct LyricPosition(int Slide, int WordsLeft, int Matched, double Confidence);

/// <summary>
/// Finds the place of a known song that a run of recognised words belongs to.
/// The recogniser makes mistakes and drops words over a band; this does not
/// need a transcript, only enough words in order to tell one slide of the
/// song from the others.
/// </summary>
public sealed class LyricFollower
{
    /// <summary>How many of the latest words are compared: about one sung line.</summary>
    public const int Window = 7;

    /// <summary>Fewer words than this say nothing yet.</summary>
    public const int MinimumHeard = 3;

    /// <summary>Lost per word skipped between two that matched, on either side.</summary>
    private const double GapPenalty = 0.12;

    /// <summary>Words that may be skipped between two matches.</summary>
    private const int MaxGap = 3;

    private readonly IReadOnlyList<string>[] _slides;
    private readonly List<string> _heard = [];

    public LyricFollower(IEnumerable<string> slideTexts)
    {
        _slides = [.. slideTexts.Select(LyricSounds.Words)];
    }

    public int SlideCount => _slides.Length;

    /// <summary>Adds what the recogniser just heard, in order.</summary>
    public void Hear(string? text)
    {
        _heard.AddRange(LyricSounds.Words(text));
        if (_heard.Count > Window)
            _heard.RemoveRange(0, _heard.Count - Window);
    }

    /// <summary>Forgets what was heard: after a change of slide, old words must not count again.</summary>
    public void Forget() => _heard.Clear();

    /// <summary>
    /// The best place for the latest words, or null when they are too few or
    /// fit nowhere. Among slides that fit the same (a chorus that comes
    /// twice), the one on screen wins, then the one after it, then the
    /// nearest ahead.
    /// </summary>
    public LyricPosition? Locate(int liveSlide)
    {
        if (_heard.Count < MinimumHeard)
            return null;

        LyricPosition? best = null;
        var bestRank = double.MinValue;

        for (var s = 0; s < _slides.Length; s++)
        {
            if (Align(_slides[s]) is not { } found)
                continue;

            var position = new LyricPosition(s, found.WordsLeft, found.Matched, found.Score / _heard.Count);
            var rank = position.Confidence + Nearness(s, liveSlide);
            if (rank > bestRank)
            {
                bestRank = rank;
                best = position;
            }
        }

        return best;
    }

    /// <summary>A small push for the slides the song is most likely on, to break ties.</summary>
    private double Nearness(int slide, int live)
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
    /// The best run of heard words found in order inside a slide. Only runs
    /// that reach the latest words count: an old match says where the singing
    /// was, not where it is.
    /// </summary>
    private (double Score, int Matched, int WordsLeft)? Align(IReadOnlyList<string> slide)
    {
        var m = _heard.Count;
        var n = slide.Count;
        if (n == 0)
            return null;

        var score = new double[m, n];
        var count = new int[m, n];
        (double Score, int Matched, int End)? best = null;

        for (var i = 0; i < m; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var alike = LyricSounds.Similarity(_heard[i], slide[j]);
                if (alike <= 0)
                    continue;

                double carried = 0;
                var carriedCount = 0;
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
                        }
                    }
                }

                score[i, j] = alike + carried;
                count[i, j] = carriedCount + 1;

                // Must end on one of the last words heard.
                if (i < m - 2)
                    continue;
                if (best is null || score[i, j] > best.Value.Score
                    || (score[i, j] == best.Value.Score && j > best.Value.End))
                    best = (score[i, j], count[i, j], j);
            }
        }

        return best is { } b ? (b.Score, b.Matched, n - 1 - b.End) : null;
    }
}
