namespace EcclesiaCast.Core.Presentation;

/// <summary>A stretch of text: where it starts and how many characters it covers.</summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public bool Contains(TextSpan other) => other.Start >= Start && other.End <= End;

    public bool Touches(TextSpan other) => other.Start <= End && other.End >= Start;
}

/// <summary>
/// What is marked with the pen over the projected text. Two kinds of mark
/// live side by side:
/// <list type="bullet">
/// <item><b>Terms</b>, typed in the Resaltar box (several, split by «;»):
/// every appearance of each one is marked.</item>
/// <item><b>Spans</b>, pointed at on the Live panel: only that appearance, by
/// its position in the main text.</item>
/// </list>
/// Immutable, with value equality, so the views redraw only when it changes.
/// </summary>
public sealed class HighlightSet : IEquatable<HighlightSet>
{
    public static HighlightSet None { get; } = new([], []);

    public HighlightSet(IEnumerable<string> terms, IEnumerable<TextSpan> spans)
    {
        Terms = terms
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        Spans = spans.Where(s => s.Start >= 0 && s.Length > 0).Distinct().ToList();
    }

    public IReadOnlyList<string> Terms { get; }

    public IReadOnlyList<TextSpan> Spans { get; }

    public bool IsEmpty => Terms.Count == 0 && Spans.Count == 0;

    public HighlightSet WithTerms(IEnumerable<string> terms) => new(terms, Spans);

    public HighlightSet WithSpans(IEnumerable<TextSpan> spans) => new(Terms, spans);

    /// <summary>
    /// The terms the Resaltar box holds when its text is <paramref name="input"/>:
    /// pieces split by «;», trimmed, blanks dropped.
    /// </summary>
    public static IReadOnlyList<string> SplitTerms(string? input) =>
        (input ?? string.Empty)
            .Split(';')
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();

    /// <summary>What goes back into the box for these terms.</summary>
    public static string JoinTerms(IEnumerable<string> terms) => string.Join("; ", terms);

    /// <summary>
    /// Every stretch of <paramref name="text"/> that is marked, in order and
    /// with overlaps joined. Spans only count for the main text (their
    /// positions mean nothing in the second Bible version below it).
    /// </summary>
    public IReadOnlyList<TextSpan> Resolve(string? text, bool includeSpans = true)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var found = new List<TextSpan>();
        foreach (var term in Terms)
            found.AddRange(FindAll(text, term));

        if (includeSpans)
        {
            foreach (var span in Spans)
            {
                if (span.Start >= text.Length)
                    continue;
                found.Add(new TextSpan(span.Start, Math.Min(span.Length, text.Length - span.Start)));
            }
        }

        found.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<TextSpan>();
        foreach (var span in found)
        {
            if (merged.Count > 0 && span.Start <= merged[^1].End)
            {
                var last = merged[^1];
                merged[^1] = new TextSpan(last.Start, Math.Max(last.End, span.End) - last.Start);
            }
            else
            {
                merged.Add(span);
            }
        }

        return merged;
    }

    /// <summary>
    /// Pointing at <paramref name="mark"/> on the Live panel (positions in
    /// <paramref name="mainText"/>). Pointing at something already marked takes
    /// the mark off; otherwise it is marked alone, or added to the others when
    /// <paramref name="addToOthers"/> (Shift) says so.
    /// </summary>
    public HighlightSet Point(TextSpan mark, string mainText, bool addToOthers)
    {
        // Already marked by pointing: pointing again takes that mark off.
        var covering = Spans.Where(s => s.Contains(mark)).ToList();
        if (covering.Count > 0)
            return WithSpans(Spans.Except(covering));

        // Marked because it was typed in the box: the typed term goes, since
        // there is no way to take off only one of its appearances.
        foreach (var term in Terms)
        {
            if (FindAll(mainText, term).Any(s => s.Contains(mark)))
                return WithTerms(Terms.Where(t => !string.Equals(t, term, StringComparison.CurrentCultureIgnoreCase)));
        }

        return WithSpans(addToOthers ? Spans.Append(mark) : [mark]);
    }

    private static IEnumerable<TextSpan> FindAll(string text, string term)
    {
        var position = 0;
        while (position < text.Length)
        {
            var index = text.IndexOf(term, position, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
                yield break;

            yield return new TextSpan(index, term.Length);
            position = index + term.Length;
        }
    }

    public bool Equals(HighlightSet? other) =>
        other is not null
        && Terms.SequenceEqual(other.Terms, StringComparer.CurrentCultureIgnoreCase)
        && Spans.SequenceEqual(other.Spans);

    public override bool Equals(object? obj) => Equals(obj as HighlightSet);

    public override int GetHashCode() => HashCode.Combine(Terms.Count, Spans.Count);
}
