using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Songs.Web;

/// <summary>
/// Reads the pages of musica.com: the quick search the site shows while
/// typing, the titles of its full (Google) search, and a lyrics page.
///
/// It is plain text matching against the site's markup, kept here so it can
/// be tested against saved snippets: when the site changes its layout this
/// is the one place that needs fixing.
/// </summary>
public static partial class MusicaComParser
{
    public const string BaseUrl = "https://www.musica.com/";

    /// <summary>The search the site runs as you type; matches titles and artists.</summary>
    public static string QuickSearchUrl(string query) =>
        $"{BaseUrl}incsmas/buscamov.asp?q={Uri.EscapeDataString(query)}&t=let";

    /// <summary>The site's full search, which also finds a song by a line of its lyrics.</summary>
    public static string FullSearchUrl(string query) =>
        $"{BaseUrl}letras.asp?q={Uri.EscapeDataString(query)}&ref=si";

    public static string LyricsUrl(string id) => $"{BaseUrl}letras.asp?letra={id}";

    [GeneratedRegex(@"musica\.com/letras\.asp\?letra=(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex LyricsLink();

    [GeneratedRegex(@"<a\s+href=""(?<href>[^""]*letras\.asp\?letra=\d+)""\s*>(?<inner>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex QuickSearchRow();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"<p\b[^>]*>(?<inner>.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Paragraph();

    [GeneratedRegex(@"^\s*<strong>[^<]*</strong>\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex OnlyBold();

    [GeneratedRegex(@"<(?<close>/?)div\b", RegexOptions.IgnoreCase)]
    private static partial Regex DivTag();

    [GeneratedRegex(@"<script[^>]*application/ld\+json[^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLd();

    [GeneratedRegex(@"<title>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PageTitle();

    [GeneratedRegex(@"\s*\((?:ft|feat)\.?\s[^)]*\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Featuring();

    /// <summary>The song id in a lyrics link, or null if it isn't one.</summary>
    public static string? LyricsId(string url)
    {
        var match = LyricsLink().Match(url);
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>
    /// Rows of the quick search. Each song is a link whose text is
    /// "Artista&lt;br&gt;Título"; the artist page and the "more results" row
    /// have no line break and are skipped.
    /// </summary>
    public static IReadOnlyList<WebSongHit> ParseQuickSearch(string html)
    {
        var hits = new List<WebSongHit>();
        var seen = new HashSet<string>();

        foreach (Match row in QuickSearchRow().Matches(html))
        {
            var id = LyricsId(row.Groups["href"].Value);
            var parts = LineBreak().Split(row.Groups["inner"].Value);
            if (id is null || parts.Length < 2 || !seen.Add(id))
                continue;

            var artist = Text(parts[0]);
            var title = Text(parts[1]);
            if (title.Length == 0)
                continue;

            hits.Add(new WebSongHit(WebSongSource.MusicaCom, CleanTitle(title), artist, LyricsUrl(id)));
        }

        return hits;
    }

    /// <summary>
    /// A result of the full search: its link and the page title, which the
    /// site writes as "Título - Letra - Artista - Musica.com".
    /// </summary>
    public static WebSongHit? ParseFullSearchResult(string href, string pageTitle)
    {
        var id = LyricsId(href);
        if (id is null)
            return null;

        var (title, artist) = SplitPageTitle(pageTitle);
        return title.Length == 0
            ? null
            : new WebSongHit(WebSongSource.MusicaCom, CleanTitle(title), artist, LyricsUrl(id));
    }

    /// <summary>Title, artist and lyrics of a song page, or null if it has no lyrics.</summary>
    public static WebSong? ParseLyricsPage(string html)
    {
        var body = LyricsBlock(html);
        if (body is null)
            return null;

        var paragraphs = new List<string>();
        foreach (Match p in Paragraph().Matches(body))
        {
            var inner = p.Groups["inner"].Value;

            // The block opens with the song title in bold, not part of the lyrics.
            if (paragraphs.Count == 0 && OnlyBold().IsMatch(inner))
                continue;

            var text = string.Join("\n", LineBreak().Split(inner).Select(Text));
            if (text.Trim().Length > 0)
                paragraphs.Add(text);
        }

        var lyrics = WebLyricsFormatter.FromParagraphs(paragraphs);
        if (lyrics.Length == 0)
            return null;

        var (title, artist) = ReadTitleAndArtist(html);
        return new WebSong(CleanTitle(title), artist, lyrics);
    }

    /// <summary>
    /// The inside of &lt;div id="letra"&gt;, found by counting nested divs:
    /// the site drops ad containers in the middle of the lyrics.
    /// </summary>
    private static string? LyricsBlock(string html)
    {
        var marker = html.IndexOf("id=\"letra\"", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return null;

        var start = html.LastIndexOf("<div", marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        var depth = 0;
        foreach (Match tag in DivTag().Matches(html, start))
        {
            depth += tag.Groups["close"].Value.Length == 0 ? 1 : -1;
            if (depth == 0)
                return html[start..tag.Index];
        }

        return html[start..];
    }

    private static (string Title, string Artist) ReadTitleAndArtist(string html)
    {
        foreach (Match script in JsonLd().Matches(html))
        {
            try
            {
                using var doc = JsonDocument.Parse(script.Groups["json"].Value);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("@type", out var type)
                    || type.GetString() != "MusicRecording")
                {
                    continue;
                }

                var title = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                var artist = root.TryGetProperty("byArtist", out var by)
                             && by.ValueKind == JsonValueKind.Object
                             && by.TryGetProperty("name", out var artistName)
                    ? artistName.GetString() ?? ""
                    : "";

                if (title.Length > 0)
                    return (WebUtility.HtmlDecode(title).Trim(), WebUtility.HtmlDecode(artist).Trim());
            }
            catch (JsonException)
            {
                // A malformed block: fall back to the page title.
            }
        }

        var pageTitle = PageTitle().Match(html);
        return pageTitle.Success ? SplitPageTitle(Text(pageTitle.Groups["title"].Value)) : ("", "");
    }

    /// <summary>"Renuévame - Letra - Marcos Witt - Musica.com" → (Renuévame, Marcos Witt).</summary>
    internal static (string Title, string Artist) SplitPageTitle(string pageTitle)
    {
        var text = WebUtility.HtmlDecode(pageTitle).Trim();

        const string suffix = " - Musica.com";
        if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            text = text[..^suffix.Length];

        const string separator = " - Letra - ";
        var at = text.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
        return at < 0
            ? (text.Trim(), "")
            : (text[..at].Trim(), text[(at + separator.Length)..].Trim());
    }

    /// <summary>Drops the "(ft. …)" tail: it does not belong in a church's song list.</summary>
    private static string CleanTitle(string title) => Featuring().Replace(title, "").Trim();

    private static string Text(string html) =>
        WebUtility.HtmlDecode(Tag().Replace(html, "")).Replace(' ', ' ').Trim();
}
