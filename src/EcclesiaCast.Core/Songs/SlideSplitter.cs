namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Cuts one block of a song into the pieces of the slides, when the operator
/// asked for at most N lines on each. The song keeps its block whole (the editor
/// still shows one paragraph); only the slides are cut.
/// </summary>
public static class SlideSplitter
{
    public const int MaxLinesLimit = 4;

    /// <summary>
    /// The text in pieces of at most <paramref name="maxLines"/> lines, in order.
    /// 0 (or anything below 1) means no limit: the text comes back whole. A block
    /// that already fits comes back whole too, untouched. The last piece takes
    /// what is left, so five lines at two a slide are 2 + 2 + 1.
    /// </summary>
    public static IReadOnlyList<string> Split(string text, int maxLines)
    {
        if (maxLines < 1 || string.IsNullOrEmpty(text))
            return [text];

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var last = lines.Length;
        while (last > 0 && string.IsNullOrWhiteSpace(lines[last - 1]))
            last--;

        maxLines = Math.Min(maxLines, MaxLinesLimit);
        if (last <= maxLines)
            return [text];

        var pieces = new List<string>();
        for (var start = 0; start < last; start += maxLines)
            pieces.Add(string.Join('\n', lines.Skip(start).Take(Math.Min(maxLines, last - start))));
        return pieces;
    }
}
