using System.Globalization;
using System.Text;
using System.Text.Json;

namespace EcclesiaCast.Core.Songs.Web;

/// <summary>
/// Reads the search results of LRCLIB (lrclib.net), a free and open lyrics
/// database with a public API. It is the fallback for songs musica.com
/// does not have, and its results already include the lyrics.
/// </summary>
public static class LrclibParser
{
    public const string BaseUrl = "https://lrclib.net/";

    /// <summary>Matches the title, the artist or the album.</summary>
    public static string SearchUrl(string query) =>
        $"{BaseUrl}api/search?q={Uri.EscapeDataString(query)}";

    /// <summary>
    /// LRCLIB lists the same song once per album it appears on, and many
    /// entries are instrumental or have no lyrics: keep one of each song
    /// that has text.
    /// </summary>
    public static IReadOnlyList<WebSongHit> ParseSearch(string json, int max = 10)
    {
        var hits = new List<WebSongHit>();
        var seen = new HashSet<string>();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return hits;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("instrumental", out var instrumental)
                && instrumental.ValueKind == JsonValueKind.True)
            {
                continue;
            }

            var lyrics = String(item, "plainLyrics");
            var title = String(item, "trackName").Trim();
            var artist = String(item, "artistName").Trim();
            if (lyrics.Trim().Length == 0 || title.Length == 0)
                continue;

            if (!seen.Add(Key(title) + "|" + Key(artist)))
                continue;

            var id = item.TryGetProperty("id", out var idValue) ? idValue.ToString() : "";
            hits.Add(new WebSongHit(
                WebSongSource.Lrclib, title, artist,
                $"{BaseUrl}api/get/{id}",
                WebLyricsFormatter.FromPlainText(lyrics)));

            if (hits.Count >= max)
                break;
        }

        return hits;
    }

    private static string String(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>"Renuévame" and "Renuevame" are the same song.</summary>
    internal static string Key(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && builder.Length > 0 && builder[^1] != ' ')
                builder.Append(' ');
        }
        return builder.ToString().Trim();
    }
}
