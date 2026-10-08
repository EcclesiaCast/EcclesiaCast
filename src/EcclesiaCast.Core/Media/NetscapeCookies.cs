using System.Text;

namespace EcclesiaCast.Core.Media;

/// <summary>One cookie as the embedded browser reports it.</summary>
public sealed record BrowserCookie(
    string Domain,
    string Path,
    string Name,
    string Value,
    DateTimeOffset? Expires,
    bool Secure,
    bool HttpOnly);

/// <summary>
/// Writes cookies in the Netscape "cookies.txt" format that yt-dlp reads with
/// <c>--cookies</c>. The browser keeps its own cookie file locked while it
/// runs, so the cookies are asked of the browser itself and handed over as
/// text instead of being read from disk.
/// </summary>
public static class NetscapeCookies
{
    /// <summary>
    /// Session cookies have no expiry; yt-dlp would read a 0 as "already
    /// expired", so they get a day of life, plenty for one download.
    /// </summary>
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(1);

    public static string Format(IEnumerable<BrowserCookie> cookies, DateTimeOffset now)
    {
        var text = new StringBuilder();
        text.Append("# Netscape HTTP Cookie File\n");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cookie in cookies)
        {
            if (string.IsNullOrEmpty(cookie.Name) || string.IsNullOrEmpty(cookie.Domain))
                continue;

            // The same cookie comes back once per URL it was asked for.
            if (!seen.Add($"{cookie.Domain}\t{cookie.Path}\t{cookie.Name}"))
                continue;

            var expires = cookie.Expires ?? now + SessionLifetime;
            var subdomains = cookie.Domain.StartsWith('.');

            // yt-dlp, like curl, marks HttpOnly cookies with this prefix.
            if (cookie.HttpOnly)
                text.Append("#HttpOnly_");

            text.Append(cookie.Domain).Append('\t')
                .Append(subdomains ? "TRUE" : "FALSE").Append('\t')
                .Append(string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path).Append('\t')
                .Append(cookie.Secure ? "TRUE" : "FALSE").Append('\t')
                .Append(expires.ToUnixTimeSeconds()).Append('\t')
                .Append(cookie.Name).Append('\t')
                // A tab or a line break inside a value would shift the columns.
                .Append(cookie.Value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')).Append('\n');
        }

        return text.ToString();
    }
}
