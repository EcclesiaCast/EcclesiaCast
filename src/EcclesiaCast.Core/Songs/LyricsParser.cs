using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Converts between the lyrics text the operator edits and the list of
/// sections that gets stored and projected.
///
/// Tagged form: lines like <c>[Coro]</c> start a new section. Untagged
/// lyrics are split on blank lines, one verse per block.
/// </summary>
public static partial class LyricsParser
{
    [GeneratedRegex(@"^\s*\[(?<label>[^\]]+)\]\s*$")]
    private static partial Regex TagLine();

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLines();

    public static List<SongSection> Parse(string? lyrics)
    {
        var normalized = (lyrics ?? string.Empty).Replace("\r\n", "\n").Trim();
        if (normalized.Length == 0)
            return [];

        var lines = normalized.Split('\n');
        if (!lines.Any(l => TagLine().IsMatch(l)))
            return ParseUntagged(normalized);

        var sections = new List<SongSection>();
        string? currentLabel = null;
        var buffer = new List<string>();

        void Flush()
        {
            var text = string.Join("\n", buffer).Trim();
            if (text.Length > 0)
            {
                sections.Add(new SongSection
                {
                    Order = sections.Count,
                    Label = currentLabel ?? "Verso",
                    Text = text,
                });
            }
            buffer.Clear();
        }

        foreach (var line in lines)
        {
            var match = TagLine().Match(line);
            if (match.Success)
            {
                Flush();
                currentLabel = match.Groups["label"].Value.Trim();
            }
            else
            {
                buffer.Add(line);
            }
        }

        Flush();
        return sections;
    }

    private static List<SongSection> ParseUntagged(string normalized)
    {
        var blocks = BlankLines().Split(normalized)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .ToList();

        return blocks
            .Select((text, i) => new SongSection
            {
                Order = i,
                Label = blocks.Count == 1 ? "Verso" : $"Verso {i + 1}",
                Text = text,
            })
            .ToList();
    }

    /// <summary>Serializes sections back to the tagged text shown in the editor.</summary>
    public static string ToTaggedText(IEnumerable<SongSection> sections) =>
        string.Join("\n\n", sections
            .OrderBy(s => s.Order)
            .Select(s => $"[{s.Label}]\n{s.Text}"));
}
