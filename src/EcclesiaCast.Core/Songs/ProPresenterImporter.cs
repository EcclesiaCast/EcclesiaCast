using System.Text;

namespace EcclesiaCast.Core.Songs;

/// <summary>
/// Imports songs from ProPresenter 7 <c>.pro</c> files. The format is a
/// binary Protocol Buffers document, but every slide's text is embedded
/// as an RTF block, so the importer scans for those blocks in order and
/// converts each one to a slide.
/// </summary>
public static class ProPresenterImporter
{
    public static Song FromFile(string fileName, byte[] content)
    {
        var sections = new List<SongSection>();

        foreach (var rtf in ExtractRtfBlocks(content))
        {
            var text = Rtf.ToPlainText(rtf);
            if (text.Length == 0)
                continue;

            // The same slide text can appear more than once in the file
            // (templates, preview copies); keep only the first occurrence
            // of consecutive duplicates.
            if (sections.Count > 0 && sections[^1].Text == text)
                continue;

            sections.Add(new SongSection
            {
                Order = sections.Count,
                Label = (sections.Count + 1).ToString(),
                Text = text,
            });
        }

        var title = Path.GetFileNameWithoutExtension(fileName).Trim();
        return new Song
        {
            Title = title.Length > 0 ? title : "Sin título",
            Artist = string.Empty,
            Sections = sections,
        };
    }

    private static IEnumerable<string> ExtractRtfBlocks(byte[] data)
    {
        var marker = "{\\rtf"u8.ToArray();

        for (var i = 0; i + marker.Length <= data.Length; i++)
        {
            if (!data.AsSpan(i, marker.Length).SequenceEqual(marker))
                continue;

            var end = FindGroupEnd(data, i);
            if (end < 0)
                yield break;

            // RTF is 7-bit with escaped unicode; Latin-1 keeps bytes 1:1.
            yield return Encoding.Latin1.GetString(data, i, end - i + 1);
            i = end;
        }
    }

    private static int FindGroupEnd(byte[] data, int start)
    {
        var depth = 0;
        var escaped = false;

        for (var j = start; j < data.Length; j++)
        {
            var b = data[j];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (b == (byte)'\\')
                escaped = true;
            else if (b == (byte)'{')
                depth++;
            else if (b == (byte)'}' && --depth == 0)
                return j;
        }

        return -1;
    }
}
