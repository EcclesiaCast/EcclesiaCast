using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs.Web;

/// <summary>
/// Turns lyrics as they come from a lyrics site into the text the song
/// editor uses: one paragraph per slide and <c>[Coro]</c>-style tag lines.
///
/// Sites write the section names inside the lyrics ("Coro:", "VERSO 1",
/// "(Puente)"); left as they are they would be projected. Long paragraphs
/// are split, because a site happily prints twelve lines in a row and that
/// does not fit on a slide.
/// </summary>
public static partial class WebLyricsFormatter
{
    /// <summary>A paragraph longer than this is split into several slides.</summary>
    public const int MaxLinesPerSlide = 6;

    /// <summary>Lines per slide when a long paragraph is split.</summary>
    private const int TargetLinesPerSlide = 4;

    [GeneratedRegex(
        @"^[\(\[]?\s*(?<label>(?:verso|estrofa|coro|pre-?coro|precoro|puente|intro|introducci[oó]n|final|outro|interludio|repetir|tag|verse|chorus|pre-?chorus|bridge)(?:\s*\d+)?)\s*[\)\]]?\s*:?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex LabelLine();

    [GeneratedRegex(
        @"^(?<label>(?:verso|estrofa|coro|pre-?coro|precoro|puente|intro|final|verse|chorus|bridge)(?:\s*\d+)?)\s*:\s*(?<text>\S.*)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex LabelWithText();

    [GeneratedRegex(@"\n[ \t]*\n\s*")]
    private static partial Regex BlankLines();

    /// <summary>Plain text with paragraphs separated by blank lines.</summary>
    public static string FromPlainText(string? text)
    {
        var normalized = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        return FromParagraphs(BlankLines().Split(normalized));
    }

    /// <summary>Paragraphs as the site laid them out, lines separated by \n.</summary>
    public static string FromParagraphs(IEnumerable<string> paragraphs)
    {
        var writer = new Writer();

        foreach (var paragraph in paragraphs)
        {
            var lines = new List<string>();

            foreach (var raw in paragraph.Split('\n'))
            {
                var line = CleanLine(raw);
                if (line.Length == 0)
                    continue;

                var label = LabelLine().Match(line);
                if (label.Success)
                {
                    writer.Slides(lines);
                    lines.Clear();
                    writer.Tag(label.Groups["label"].Value);
                    continue;
                }

                var inline = LabelWithText().Match(line);
                if (inline.Success)
                {
                    writer.Slides(lines);
                    lines.Clear();
                    writer.Tag(inline.Groups["label"].Value);
                    line = inline.Groups["text"].Value.Trim();
                }

                lines.Add(line);
            }

            writer.Slides(lines);
        }

        return writer.ToString();
    }

    private static string CleanLine(string raw)
    {
        var line = raw.Replace(' ', ' ').Trim();
        while (line.Contains("  ", StringComparison.Ordinal))
            line = line.Replace("  ", " ", StringComparison.Ordinal);
        return line;
    }

    private sealed class Writer
    {
        private readonly StringBuilder _text = new();

        /// <summary>True right after a tag line: the slide goes right under it.</summary>
        private bool _afterTag;

        public void Tag(string label)
        {
            if (_text.Length > 0)
                _text.Append('\n');
            _text.Append('[').Append(Capitalize(label.Trim())).Append("]\n");
            _afterTag = true;
        }

        /// <summary>Writes one site paragraph as one or more slides.</summary>
        public void Slides(List<string> lines)
        {
            if (lines.Count == 0)
                return;

            foreach (var chunk in Split(lines))
            {
                if (_text.Length > 0 && !_afterTag)
                    _text.Append('\n');
                _text.AppendJoin('\n', chunk).Append('\n');
                _afterTag = false;
            }
        }

        public override string ToString() => _text.ToString().TrimEnd();
    }

    /// <summary>
    /// Splits a long paragraph into balanced slides: ten lines become
    /// 4 + 3 + 3, not 4 + 4 + 2 with an orphan pair at the end.
    /// </summary>
    internal static IEnumerable<IReadOnlyList<string>> Split(IReadOnlyList<string> lines)
    {
        if (lines.Count <= MaxLinesPerSlide)
        {
            yield return lines;
            yield break;
        }

        var slides = (lines.Count + TargetLinesPerSlide - 1) / TargetLinesPerSlide;
        var start = 0;
        for (var i = 0; i < slides; i++)
        {
            var size = (lines.Count - start) / (slides - i);
            if ((lines.Count - start) % (slides - i) != 0)
                size++;
            yield return lines.Skip(start).Take(size).ToList();
            start += size;
        }
    }

    private static string Capitalize(string label)
    {
        var lower = label.ToLowerInvariant();
        return lower.Length == 0 ? lower : char.ToUpperInvariant(lower[0]) + lower[1..];
    }
}
