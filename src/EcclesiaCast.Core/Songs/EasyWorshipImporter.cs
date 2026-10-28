using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Turns one song of an EasyWorship database into a <see cref="Song"/>. The
/// database itself is read by <c>EasyWorshipDatabase</c> (in Data); this part
/// only knows how the words are written: an RTF text where each slide is a
/// paragraph, and a line such as «Verse 1» or «Coro» may open a block.
/// </summary>
public static partial class EasyWorshipImporter
{
    [GeneratedRegex(@"^\s*(?<name>verse|chorus|pre-?chorus|bridge|intro|outro|ending|tag|interlude|refrain|verso|coro|pre-?coro|estribillo|puente|final|interludio)\s*(?<number>\d+)?\s*:?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex LabelLine();

    /// <param name="wordsRtf">The words of the song, one RTF text per row of the words table, in order.</param>
    public static Song FromRtf(string title, string? author, string? copyright, IEnumerable<string> wordsRtf)
    {
        var blocks = new List<string>();
        foreach (var rtf in wordsRtf)
        {
            var text = rtf.TrimStart().StartsWith("{\\rtf", StringComparison.Ordinal)
                ? Rtf.ToPlainText(rtf)
                : rtf;
            text = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            if (text.Length > 0)
                blocks.Add(text);
        }

        var lyrics = string.Join("\n\n", blocks.Select(MarkLabel));
        var name = title.Trim();

        return new Song
        {
            Title = name.Length > 0 ? name : "Sin título",
            Artist = (author ?? string.Empty).Trim(),
            Copyright = string.IsNullOrWhiteSpace(copyright) ? null : copyright.Trim(),
            Sections = LyricsParser.Parse(lyrics),
        };
    }

    /// <summary>
    /// A paragraph whose first line is only a label («Verse 1», «Coro») becomes
    /// «[Verse 1]» over its words, which is how the editor writes a label.
    /// A paragraph that is nothing but a label is left as it was.
    /// </summary>
    private static string MarkLabel(string block)
    {
        var marked = new StringBuilder();
        foreach (var paragraph in Regex.Split(block, @"\n\s*\n"))
        {
            var lines = paragraph.Split('\n');
            if (marked.Length > 0)
                marked.Append("\n\n");

            var match = lines.Length > 1 ? LabelLine().Match(lines[0]) : Match.Empty;
            if (match.Success)
            {
                var label = (match.Groups["name"].Value + " " + match.Groups["number"].Value).Trim();
                marked.Append('[').Append(label).Append("]\n").Append(string.Join('\n', lines.Skip(1)));
            }
            else
            {
                marked.Append(paragraph);
            }
        }

        return marked.ToString();
    }
}
