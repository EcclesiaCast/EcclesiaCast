namespace EcclesiaCast.Core.Web;

/// <summary>
/// What the address box of the web panel does with what was typed, the way a
/// browser's does: an address is opened, anything else is searched for. The
/// operator types «juan 3 16 comentario» as readily as «google.com».
/// </summary>
public static class AddressInput
{
    private const string SearchUrl = "https://www.google.com/search?q=";

    /// <summary>
    /// The page to open for what was typed, or null when nothing was typed.
    /// A full http(s) address is kept as it is; «google.com» or «www.x.org»
    /// get «https://» in front; whatever else is a search on Google. Other
    /// schemes (file:, javascript:…) are never opened: they are searched like
    /// any other text.
    /// </summary>
    public static Uri? Resolve(string? text)
    {
        var typed = text?.Trim();
        if (string.IsNullOrEmpty(typed))
            return null;

        if (Uri.TryCreate(typed, UriKind.Absolute, out var full)
            && full.Scheme is "http" or "https"
            && !string.IsNullOrEmpty(full.Host))
            return full;

        if (LooksLikeAddress(typed))
        {
            // Nothing listens on https on the operator's own computer or
            // network, so those go in the clear, as every browser ends up doing.
            var scheme = IsLocal(typed) ? "http" : "https";
            if (Uri.TryCreate($"{scheme}://{typed}", UriKind.Absolute, out var completed))
                return completed;
        }

        return new Uri(SearchUrl + Uri.EscapeDataString(typed));
    }

    /// <summary>
    /// A short name for a page that has no title: its host without «www.».
    /// </summary>
    public static string DisplayName(string? address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            return string.IsNullOrWhiteSpace(address) ? "Página web" : address.Trim();

        var host = uri.Host;
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    /// <summary>
    /// True when the text is a host name (with an optional port, path, query
    /// or fragment) and not words: no spaces, dots between names, and a last
    /// name that is a real ending («com», «org», «ar»…), not a number.
    /// </summary>
    private static bool LooksLikeAddress(string text)
    {
        if (text.Any(char.IsWhiteSpace))
            return false;

        var end = text.IndexOfAny(['/', '?', '#']);
        var authority = end < 0 ? text : text[..end];
        var host = StripPort(authority);
        if (host.Length == 0)
            return false;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IsIPv4(host))
            return true;

        var labels = host.Split('.');
        if (labels.Length < 2 || labels.Any(l => l.Length == 0 || !IsHostLabel(l)))
            return false;

        // «3.16» or «juan3.16»: a number after the dot is a reference, not a domain.
        var ending = labels[^1];
        return ending.Length >= 2 && ending.All(c => char.IsLetter(c) || c == '-');
    }

    private static bool IsLocal(string text)
    {
        var end = text.IndexOfAny(['/', '?', '#']);
        var host = StripPort(end < 0 ? text : text[..end]);
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IsIPv4(host);
    }

    private static string StripPort(string authority)
    {
        var colon = authority.LastIndexOf(':');
        if (colon < 0)
            return authority;

        // Only a numeric port: «mailto:x» or «a:b» are not an address.
        var port = authority[(colon + 1)..];
        return port.Length > 0 && port.All(char.IsAsciiDigit) ? authority[..colon] : string.Empty;
    }

    private static bool IsIPv4(string host)
    {
        var parts = host.Split('.');
        return parts.Length == 4
               && parts.All(p => p.Length is > 0 and <= 3 && p.All(char.IsAsciiDigit) && int.Parse(p) <= 255);
    }

    private static bool IsHostLabel(string label) =>
        label.All(c => char.IsLetterOrDigit(c) || c == '-')
        && label[0] != '-' && label[^1] != '-';
}
