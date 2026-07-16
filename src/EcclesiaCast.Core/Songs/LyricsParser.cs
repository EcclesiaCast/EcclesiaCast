using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Converts between the lyrics text the operator edits and the list of
/// sections (slides) that gets stored and projected.
///
/// Every non-empty line is one slide. Blank lines are ignored. A line like
/// <c>[Coro]</c> is optional: it does not become a slide, it only labels
/// the lines that follow it. Lines without a label are numbered.
/// </summary>
public static partial class LyricsParser
{
    [GeneratedRegex(@"^\s*\[(?<label>[^\]]+)\]\s*$")]
    private static partial Regex TagLine();

    public static List<SongSection> Parse(string? lyrics)
    {
        var normalized = (lyrics ?? string.Empty).Replace("\r\n", "\n");
        var sections = new List<SongSection>();
        string? label = null;

        foreach (var raw in normalized.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var match = TagLine().Match(line);
            if (match.Success)
            {
                label = match.Groups["label"].Value.Trim();
                continue;
            }

            sections.Add(new SongSection
            {
                Order = sections.Count,
                Label = label ?? (sections.Count + 1).ToString(),
                Text = line,
            });
        }

        return sections;
    }

    /// <summary>Serializes sections back to the text shown in the editor.</summary>
    public static string ToTaggedText(IEnumerable<SongSection> sections)
    {
        var builder = new StringBuilder();
        string? currentLabel = null;

        foreach (var section in sections.OrderBy(s => s.Order))
        {
            // Numeric labels were auto-assigned; they don't need a tag line.
            var isAutoLabel = int.TryParse(section.Label, out _);
            if (!isAutoLabel && section.Label != currentLabel)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append('[').Append(section.Label).Append("]\n");
                currentLabel = section.Label;
            }

            builder.Append(section.Text).Append('\n');
        }

        return builder.ToString().TrimEnd();
    }
}
