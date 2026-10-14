using System.Text;

namespace EcclesiaCast.Core.Web;

/// <summary>
/// The pure parts of bringing a picture in from Google Images: the address of
/// a search, what kind of file the bytes are, and a file name that is safe to
/// write. The network and the disk stay with the caller.
/// </summary>
public static class WebImages
{
    /// <summary>Pictures bigger than this are not taken: it is a photo from a web page, not a video.</summary>
    public const long MaxBytes = 40L * 1024 * 1024;

    /// <summary>Under this width a picture is too small for a big screen and the operator is told.</summary>
    public const int SmallWidth = 800;

    /// <summary>
    /// The Google Images page for <paramref name="query"/>, in Spanish.
    /// <paramref name="animated"/> keeps only animated GIFs; <paramref name="freeLicense"/>
    /// keeps only pictures Google lists as free to use and share (it is its
    /// «Derechos de uso» filter, not a guarantee).
    /// </summary>
    public static Uri SearchUrl(string query, bool animated, bool freeLicense)
    {
        var tools = new List<string>();
        if (animated)
            tools.Add("itp:animated");
        if (freeLicense)
            tools.Add("sur:f");

        var url = new StringBuilder("https://www.google.com/search?tbm=isch&hl=es&q=")
            .Append(Uri.EscapeDataString(query.Trim()));
        if (tools.Count > 0)
            url.Append("&tbs=").Append(string.Join(',', tools));

        return new Uri(url.ToString());
    }

    /// <summary>
    /// The extension (with its dot) of what the bytes really are, whatever the
    /// server said they were; null when it is not a picture the program takes.
    /// </summary>
    public static string? ExtensionFor(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
            return ".jpg";

        if (head.Length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
            return ".png";

        if (head.Length >= 6 && head[0] == 'G' && head[1] == 'I' && head[2] == 'F' && head[3] == '8')
            return ".gif";

        if (head.Length >= 12
            && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F'
            && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
            return ".webp";

        if (head.Length >= 2 && head[0] == 'B' && head[1] == 'M')
            return ".bmp";

        return null;
    }

    /// <summary>
    /// A file name (no extension) made of what was searched for: only letters,
    /// digits, spaces and a few marks, short, never empty.
    /// </summary>
    public static string FileNameFor(string? query, string fallback = "Imagen de Google")
    {
        var clean = new StringBuilder();
        foreach (var c in query ?? string.Empty)
        {
            if (char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')
                clean.Append(c);
        }

        var name = string.Join(' ', clean.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('.', ' ');
        if (name.Length > 40)
            name = name[..40].TrimEnd();

        return name.Length == 0 ? fallback : name;
    }

    /// <summary>«name.jpg», or «name (2).jpg» when that one is taken: the first free one.</summary>
    public static string UniqueName(string name, string extension, Func<string, bool> exists)
    {
        var candidate = name + extension;
        for (var n = 2; exists(candidate); n++)
            candidate = $"{name} ({n}){extension}";

        return candidate;
    }

    /// <summary>
    /// The bytes of a <c>data:</c> address (Google puts the thumbnails in the
    /// page itself); null when it is not one, or is not base64.
    /// </summary>
    public static byte[]? FromDataUri(string? address)
    {
        if (address is null || !address.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return null;

        var comma = address.IndexOf(',');
        if (comma < 0 || !address[..comma].Contains(";base64", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            return Convert.FromBase64String(address[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
