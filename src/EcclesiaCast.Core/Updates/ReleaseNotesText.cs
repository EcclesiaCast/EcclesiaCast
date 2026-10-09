using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Updates;

/// <summary>
/// Turns the notes of a release, which are Markdown, into text a person can
/// read in a plain window: no # signs, no asterisks, no links in brackets. No
/// HTML and no web view are involved, so a release can never put anything but
/// text on the operator's screen.
/// </summary>
public static partial class ReleaseNotesText
{
    /// <summary>
    /// The release workflow writes a line about the signing policy at the top
    /// of every release, for people who read the GitHub page. This marker
    /// comes right after it, so the program shows only what follows.
    /// </summary>
    public const string NotesMarker = "<!-- notas -->";

    /// <summary>The notes of a release as shown in the update window.</summary>
    public static string FromRelease(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        var marker = body.IndexOf(NotesMarker, StringComparison.Ordinal);
        return ToPlainText(marker >= 0 ? body[(marker + NotesMarker.Length)..] : body);
    }

    public static string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var text = HtmlComment().Replace(markdown.Replace("\r\n", "\n").Replace('\r', '\n'), string.Empty);

        var output = new List<string>();
        var paragraph = new StringBuilder();
        var inFence = false;
        var afterHeading = false;

        void Flush()
        {
            if (paragraph.Length == 0)
                return;

            // Only the end is trimmed: a nested bullet starts with its indent.
            var line = Inline(paragraph.ToString().TrimEnd());
            paragraph.Clear();

            // GitHub's automatic notes end with a link to the comparison of two
            // tags. It means nothing to someone running the program.
            if (!line.StartsWith("Full Changelog", StringComparison.OrdinalIgnoreCase))
                output.Add(line);
        }

        void Blank()
        {
            Flush();
            if (output.Count > 0 && output[^1].Length > 0)
                output.Add(string.Empty);
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd();

            if (FenceStart().IsMatch(line))
            {
                Blank();
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                output.Add(line);
                continue;
            }

            if (line.Trim().Length == 0)
            {
                if (!afterHeading)
                    Blank();
                afterHeading = false;
                continue;
            }

            afterHeading = false;

            var quote = Quote().Match(line);
            if (quote.Success)
                line = quote.Groups[1].Value;

            if (Rule().IsMatch(line))
            {
                Blank();
                continue;
            }

            var heading = Heading().Match(line);
            if (heading.Success)
            {
                Blank();
                output.Add(Inline(heading.Groups[1].Value.Trim()));
                afterHeading = true;
                continue;
            }

            var bullet = Bullet().Match(line);
            if (bullet.Success)
            {
                Flush();
                var nested = bullet.Groups[1].Value.Length >= 2;
                paragraph.Append(nested ? "    – " : "• ").Append(bullet.Groups[2].Value.Trim());
                continue;
            }

            // A line that carries on the previous one (the text is wrapped at
            // 80 columns in the file) joins it, so the window wraps it to its
            // own width instead of breaking the paragraph at odd places.
            if (paragraph.Length > 0)
                paragraph.Append(' ');
            paragraph.Append(line.Trim());
        }

        Flush();

        while (output.Count > 0 && output[^1].Length == 0)
            output.RemoveAt(output.Count - 1);

        return string.Join("\n", output).Trim('\n');
    }

    /// <summary>Emphasis, links and code inside one paragraph.</summary>
    private static string Inline(string text)
    {
        // Code and escaped characters go first and are put aside: what is
        // inside them is literal, so their asterisks and underscores must
        // survive the cleaning below.
        var literal = new List<string>();
        string Hide(string value)
        {
            literal.Add(value);
            return $"{literal.Count - 1}";
        }

        text = CodeSpan().Replace(text, match => Hide(match.Groups[1].Value));
        text = Escaped().Replace(text, match => Hide(match.Groups[1].Value));

        text = Image().Replace(text, "$1");
        text = Link().Replace(text, "$1");
        text = AutoLink().Replace(text, "$1");
        text = Bold().Replace(text, "$1$2");
        text = Italic().Replace(text, "$1$2");
        text = Strike().Replace(text, "$1");
        text = HtmlTag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);

        return Placeholder().Replace(text, match => literal[int.Parse(match.Groups[1].Value)]);
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlComment();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FenceStart();

    [GeneratedRegex(@"^\s{0,3}>\s?(.*)$")]
    private static partial Regex Quote();

    [GeneratedRegex(@"^\s{0,3}([-*_])(\s*\1){2,}\s*$")]
    private static partial Regex Rule();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+(.*?)\s*#*\s*$")]
    private static partial Regex Heading();

    [GeneratedRegex(@"^(\s*)[-*+]\s+(.*)$")]
    private static partial Regex Bullet();

    [GeneratedRegex(@"`+([^`]+?)`+")]
    private static partial Regex CodeSpan();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex Image();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)\s]*(?:\s+""[^""]*"")?\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"<(https?://[^>\s]+)>")]
    private static partial Regex AutoLink();

    [GeneratedRegex(@"\*\*(?=\S)(.+?)(?<=\S)\*\*|__(?=\S)(.+?)(?<=\S)__")]
    private static partial Regex Bold();

    [GeneratedRegex(@"(?<![\*\w])\*(?![\s\*])(.+?)(?<![\s\*])\*(?![\*\w])|(?<![\w])_(?![\s_])(.+?)(?<![\s_])_(?![\w])")]
    private static partial Regex Italic();

    [GeneratedRegex(@"~~(.+?)~~")]
    private static partial Regex Strike();

    [GeneratedRegex(@"</?[a-zA-Z][^>]*>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\\([\\`*_{}\[\]()#+\-.!>~|])")]
    private static partial Regex Escaped();

    [GeneratedRegex("(\\d+)")]
    private static partial Regex Placeholder();
}
