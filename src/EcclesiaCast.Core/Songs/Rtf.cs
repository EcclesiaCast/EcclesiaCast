using System.Globalization;
using System.Text;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Minimal RTF-to-plain-text converter, sufficient for the text blocks
/// that ProPresenter embeds in its files.
/// </summary>
internal static class Rtf
{
    private static readonly HashSet<string> SkipGroups =
    [
        "fonttbl", "colortbl", "expandedcolortbl", "listtable",
        "listoverridetable", "stylesheet", "info", "pict", "themedata",
    ];

    public static string ToPlainText(string rtf)
    {
        var text = new StringBuilder();
        var i = 0;
        var depth = 0;
        var skipDepth = -1;
        var unicodeFallbackLength = 1;

        while (i < rtf.Length)
        {
            var c = rtf[i];

            if (c == '{')
            {
                depth++;
                i++;
                if (skipDepth < 0 && StartsSkippableGroup(rtf, i))
                    skipDepth = depth;
                continue;
            }

            if (c == '}')
            {
                if (skipDepth == depth)
                    skipDepth = -1;
                depth--;
                i++;
                continue;
            }

            if (skipDepth >= 0 && depth >= skipDepth)
            {
                i++;
                continue;
            }

            if (c == '\\')
            {
                i++;
                if (i >= rtf.Length)
                    break;

                var next = rtf[i];

                if (next is '{' or '}' or '\\')
                {
                    text.Append(next);
                    i++;
                    continue;
                }

                if (next == '\'')
                {
                    // \'hh — single byte in the document code page.
                    if (i + 2 < rtf.Length
                        && byte.TryParse(rtf.AsSpan(i + 1, 2), NumberStyles.HexNumber, null, out var b))
                    {
                        text.Append((char)b);
                        i += 3;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }

                if (!char.IsLetter(next))
                {
                    i++;
                    continue;
                }

                var wordStart = i;
                while (i < rtf.Length && char.IsLetter(rtf[i]))
                    i++;
                var word = rtf[wordStart..i];

                var parameter = 0;
                if (i < rtf.Length && (rtf[i] == '-' || char.IsDigit(rtf[i])))
                {
                    var negative = rtf[i] == '-';
                    if (negative)
                        i++;
                    var digitsStart = i;
                    while (i < rtf.Length && char.IsDigit(rtf[i]))
                        i++;
                    parameter = int.Parse(rtf[digitsStart..i]);
                    if (negative)
                        parameter = -parameter;
                }

                if (i < rtf.Length && rtf[i] == ' ')
                    i++;

                switch (word)
                {
                    case "par" or "line":
                        text.Append('\n');
                        break;
                    case "tab":
                        text.Append(' ');
                        break;
                    case "uc":
                        unicodeFallbackLength = parameter;
                        break;
                    case "u":
                        var code = parameter < 0 ? parameter + 65536 : parameter;
                        text.Append((char)code);
                        i = SkipUnicodeFallback(rtf, i, unicodeFallbackLength);
                        break;
                }
                continue;
            }

            if (c is not ('\r' or '\n'))
                text.Append(c);
            i++;
        }

        return text.ToString().Trim();
    }

    private static bool StartsSkippableGroup(string rtf, int i)
    {
        if (i >= rtf.Length || rtf[i] != '\\')
            return false;
        i++;

        if (i < rtf.Length && rtf[i] == '*')
            return true;

        var start = i;
        while (i < rtf.Length && char.IsLetter(rtf[i]))
            i++;
        return SkipGroups.Contains(rtf[start..i]);
    }

    private static int SkipUnicodeFallback(string rtf, int i, int count)
    {
        for (var k = 0; k < count && i < rtf.Length; k++)
        {
            if (rtf[i] == '\\' && i + 3 < rtf.Length && rtf[i + 1] == '\'')
                i += 4;
            else
                i++;
        }
        return i;
    }
}
