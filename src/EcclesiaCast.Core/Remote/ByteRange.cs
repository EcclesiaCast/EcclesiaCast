namespace EcclesiaCast.Core.Remote;

/// <summary>
/// The «Range: bytes=…» a browser sends to play a video from the middle (and
/// to loop it): which bytes of a file of a given length it asks for.
/// </summary>
public static class ByteRange
{
    /// <summary>
    /// The first and last byte (both included) asked for, or null when there is
    /// no range, it is not in bytes, asks for several pieces, or does not fit the
    /// file (the caller then sends the whole file, or answers «416» for one that
    /// starts past the end).
    /// </summary>
    public static (long Start, long End)? Parse(string? header, long length)
    {
        if (string.IsNullOrWhiteSpace(header) || length <= 0)
            return null;

        var text = header.Trim();
        if (!text.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return null;

        var spec = text[6..].Trim();
        if (spec.Contains(','))
            return null;

        var dash = spec.IndexOf('-');
        if (dash < 0)
            return null;

        var from = spec[..dash].Trim();
        var to = spec[(dash + 1)..].Trim();

        long start, end;
        if (from.Length == 0)
        {
            // «-500»: the last 500 bytes.
            if (!long.TryParse(to, out var last) || last <= 0)
                return null;

            start = Math.Max(0, length - last);
            end = length - 1;
        }
        else
        {
            if (!long.TryParse(from, out start) || start < 0)
                return null;

            if (to.Length == 0)
            {
                end = length - 1;
            }
            else if (!long.TryParse(to, out end) || end < start)
            {
                return null;
            }
        }

        if (start >= length)
            return null;

        return (start, Math.Min(end, length - 1));
    }

    /// <summary>True when the header is a range that starts past the end of the file («416 Range Not Satisfiable»).</summary>
    public static bool IsPastTheEnd(string? header, long length)
    {
        if (string.IsNullOrWhiteSpace(header) || !header.Trim().StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return false;

        var spec = header.Trim()[6..];
        var dash = spec.IndexOf('-');
        return dash > 0 && long.TryParse(spec[..dash].Trim(), out var start) && start >= length;
    }
}
