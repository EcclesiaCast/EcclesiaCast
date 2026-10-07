using System.Text.RegularExpressions;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.Core.Bible;

/// <summary>
/// What one slide of a chapter shows: usually a single verse, but a run of
/// verses when the translation tells them as one paragraph.
/// </summary>
public sealed record VerseGroup(int Chapter, int FirstVerse, int LastVerse, string Text, string? SecondaryText)
{
    public bool Contains(int verse) => verse >= FirstVerse && verse <= LastVerse;

    /// <summary>"16", or "2-18" for a run.</summary>
    public string Numbers => FirstVerse == LastVerse ? $"{FirstVerse}" : $"{FirstVerse}-{LastVerse}";
}

/// <summary>
/// Turns a chapter's verses into slides. Some translations (the TLA, for one)
/// retell several verses as a single paragraph: the files repeat that
/// paragraph under every verse it covers and start it with the verses' own
/// numbers, «3-4 (4-5) Después de eso…». Shown verse by verse that put the
/// same text on screen again and again, numbers included.
/// </summary>
public static partial class VerseGrouping
{
    public static IReadOnlyList<VerseGroup> Group(
        IReadOnlyList<BibleVerseResult> verses,
        IReadOnlyDictionary<(int Chapter, int Verse), string>? secondary = null)
    {
        var groups = new List<VerseGroup>();
        Run? run = null;

        foreach (var verse in verses)
        {
            string? second = null;
            secondary?.TryGetValue((verse.Chapter, verse.Verse), out second);

            // A run goes on while either version keeps repeating itself: with
            // the TLA as the second version, each verse of the first one would
            // otherwise carry the whole TLA paragraph again.
            var continues = run is not null
                && verse.Chapter == run.Chapter
                && verse.Verse == run.LastVerse + 1
                && (verse.Text == run.LastPrimary || (second is not null && second == run.LastSecondary));

            if (!continues)
            {
                if (run is not null)
                    groups.Add(run.ToGroup());
                run = new Run(verse.Chapter, verse.Verse);
            }

            run!.Add(verse.Verse, verse.Text, second);
        }

        if (run is not null)
            groups.Add(run.ToGroup());

        return groups;
    }

    /// <summary>
    /// Drops the verse numbers a translation writes at the start of the text
    /// («3-4 (4-5) », «2 (3) »), but only when they are this verse's own: a
    /// verse that simply starts with a number («72.000 cabezas de ganado»)
    /// keeps it.
    /// </summary>
    public static string StripVerseNumbers(string text, int verse)
    {
        var match = LeadingNumbers().Match(text);
        if (!match.Success || (!match.Groups["to"].Success && !match.Groups["alt"].Success))
            return text;

        var from = int.Parse(match.Groups["from"].Value);
        var to = match.Groups["to"].Success ? int.Parse(match.Groups["to"].Value) : from;
        return verse >= from && verse <= to ? text[match.Length..] : text;
    }

    [GeneratedRegex(@"^(?<from>\d+)[a-z]?(?:\s*[-–]\s*(?<to>\d+)[a-z]?)?(?<alt>\s*\(\d+(?:[.:]\d+)?[a-z]?(?:\s*[-–]\s*\d+(?:[.:]\d+)?[a-z]?)?\))?\s+")]
    private static partial Regex LeadingNumbers();

    private sealed class Run(int chapter, int firstVerse)
    {
        private readonly List<string> _primary = [];
        private readonly List<string> _secondary = [];

        public int Chapter { get; } = chapter;
        public int FirstVerse { get; } = firstVerse;
        public int LastVerse { get; private set; } = firstVerse;
        public string? LastPrimary { get; private set; }
        public string? LastSecondary { get; private set; }

        public void Add(int verse, string primary, string? secondary)
        {
            LastVerse = verse;

            if (primary != LastPrimary)
                _primary.Add(StripVerseNumbers(primary, verse));
            LastPrimary = primary;

            if (secondary is not null && secondary != LastSecondary)
                _secondary.Add(StripVerseNumbers(secondary, verse));
            LastSecondary = secondary ?? LastSecondary;
        }

        public VerseGroup ToGroup() => new(
            Chapter, FirstVerse, LastVerse,
            string.Join(" ", _primary),
            _secondary.Count > 0 ? string.Join(" ", _secondary) : null);
    }
}
