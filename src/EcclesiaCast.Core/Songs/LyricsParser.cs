using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Converts between the lyrics text the operator edits and the list of
/// sections (slides) that gets stored and projected.
///
/// Each paragraph — a block of lines separated by a blank line — is one
/// slide. A line like <c>[Coro]</c> is optional: it does not become a
/// slide, it only labels the paragraphs that follow it. Paragraphs
/// without a label are numbered.
/// </summary>
public static partial class LyricsParser
{
    [GeneratedRegex(@"^\s*\[(?<label>[^\]]+)\]\s*$")]
    private static partial Regex TagLine();

    public static List<SongSection> Parse(string? lyrics)
    {
        var normalized = (lyrics ?? string.Empty).Replace("\r\n", "\n").Trim();
        if (normalized.Length == 0)
            return [];

        var sections = new List<SongSection>();
        string? label = null;
        var buffer = new List<string>();

        void Flush()
        {
            var text = string.Join("\n", buffer).Trim();
            if (text.Length > 0)
            {
                sections.Add(new SongSection
                {
                    Order = sections.Count,
                    Label = label ?? (sections.Count + 1).ToString(),
                    Text = text,
                });
            }
            buffer.Clear();
        }

        foreach (var raw in normalized.Split('\n'))
        {
            var line = raw.Trim();

            var match = TagLine().Match(line);
            if (match.Success)
            {
                Flush();
                label = match.Groups["label"].Value.Trim();
                continue;
            }

            if (line.Length == 0)
            {
                Flush();
                continue;
            }

            buffer.Add(line);
        }

        Flush();
        return sections;
    }

    /// <summary>Serializes sections back to the text shown in the editor.</summary>
    public static string ToTaggedText(IEnumerable<SongSection> sections)
    {
        var builder = new StringBuilder();
        string? currentLabel = null;

        foreach (var section in sections.OrderBy(s => s.Order))
        {
            if (builder.Length > 0)
                builder.Append('\n');

            // Numeric labels were auto-assigned; they don't need a tag line.
            var isAutoLabel = int.TryParse(section.Label, out _);
            if (!isAutoLabel && section.Label != currentLabel)
            {
                builder.Append('[').Append(section.Label).Append("]\n");
                currentLabel = section.Label;
            }

            builder.Append(section.Text).Append('\n');
        }

        return builder.ToString().TrimEnd();
    }
}
