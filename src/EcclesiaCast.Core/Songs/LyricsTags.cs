using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// One change to the lyrics text of the editor: <see cref="Length"/>
/// characters from <see cref="Start"/> become <see cref="Text"/>, and the
/// cursor ends at <see cref="Caret"/>.
/// </summary>
public sealed record LyricsEdit(int Start, int Length, string Text, int Caret)
{
    public string ApplyTo(string lyrics) => lyrics[..Start] + Text + lyrics[(Start + Length)..];
}

/// <summary>
/// The tag buttons of the song editor («Coro», «Estrofa»…): they write the
/// <c>[Coro]</c> line that <see cref="LyricsParser"/> reads, so the operator
/// does not have to know the brackets exist (15/11: a user had loaded all his
/// songs and found no way to tell the chorus from the verses).
/// </summary>
public static partial class LyricsTags
{
    public const string Chorus = "Coro";
    public const string Verse = "Estrofa";
    public const string Bridge = "Puente";
    public const string Ending = "Final";

    [GeneratedRegex(@"^\s*\[[^\]]+\]\s*$")]
    private static partial Regex TagLine();

    [GeneratedRegex(@"^\s*\[\s*Estrofa(?:\s+(?<n>\d+))?\s*\]\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex VerseLine();

    /// <summary>
    /// «Estrofa N» for the next verse: one more than the highest already in
    /// the text. A line left out with <paramref name="skipLineAt"/> (the tag
    /// about to be replaced) does not count.
    /// </summary>
    public static string NextVerse(string? lyrics, int skipLineAt = -1)
    {
        var text = lyrics ?? string.Empty;
        var highest = 0;
        var start = 0;
        while (start <= text.Length)
        {
            var end = LineEnd(text, start);
            if (start != skipLineAt)
            {
                var match = VerseLine().Match(Line(text, start));
                if (match.Success)
                {
                    var number = match.Groups["n"].Success && int.TryParse(match.Groups["n"].Value, out var n) ? n : 1;
                    highest = Math.Max(highest, number);
                }
            }

            if (end >= text.Length)
                break;
            start = end + 1;
        }

        return $"{Verse} {highest + 1}";
    }

    /// <summary>
    /// Puts the tag for <paramref name="kind"/> (one of the constants above)
    /// on a line of its own above the paragraph the cursor is in; «Estrofa»
    /// numbers itself. A paragraph that already starts with a tag has it
    /// replaced. On a blank line the tag goes right there, with a blank line
    /// before it when the line above has text, and the cursor is left below
    /// it, ready to type.
    /// </summary>
    public static LyricsEdit Insert(string? lyrics, int caret, string kind, string? newLine = null)
    {
        var text = lyrics ?? string.Empty;
        caret = Math.Clamp(caret, 0, text.Length);
        var nl = text.Contains("\r\n") ? "\r\n" : text.Contains('\n') ? "\n" : newLine ?? Environment.NewLine;

        string Tag(int skip = -1) =>
            "[" + (kind == Verse ? NextVerse(text, skip) : kind) + "]";

        var line = LineStart(text, caret);
        if (Line(text, line).Trim().Length == 0)
        {
            var above = line > 0 && Line(text, LineStart(text, line - 1)).Trim().Length > 0;
            var added = (above ? nl : string.Empty) + Tag() + nl;
            return new LyricsEdit(line, 0, added, line + added.Length);
        }

        // Up to the first line of the paragraph.
        var first = line;
        while (first > 0)
        {
            var previous = LineStart(text, first - 1);
            if (Line(text, previous).Trim().Length == 0)
                break;
            first = previous;
        }

        var firstLine = Line(text, first);
        if (TagLine().IsMatch(firstLine))
        {
            var tag = Tag(first);
            var moved = caret <= first + firstLine.Length
                ? first + tag.Length
                : caret + tag.Length - firstLine.Length;
            return new LyricsEdit(first, firstLine.Length, tag, moved);
        }

        var inserted = Tag() + nl;
        return new LyricsEdit(first, 0, inserted, caret + inserted.Length);
    }

    private static int LineStart(string text, int index) =>
        index <= 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;

    /// <summary>Index of the line's '\n', or the end of the text.</summary>
    private static int LineEnd(string text, int start)
    {
        var end = text.IndexOf('\n', start);
        return end < 0 ? text.Length : end;
    }

    /// <summary>The line without its line break.</summary>
    private static string Line(string text, int start)
    {
        var end = LineEnd(text, start);
        if (end > start && text[end - 1] == '\r')
            end--;
        return text[start..end];
    }
}
