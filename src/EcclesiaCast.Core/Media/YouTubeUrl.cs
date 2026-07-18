using System.Text.RegularExpressions;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// Extracts the video id from the many shapes a YouTube link can take, so the
/// operator can paste whatever they copied from the browser or the phone.
/// </summary>
public static partial class YouTubeUrl
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex BareId();

    [GeneratedRegex(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|live/|shorts/|v/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})",
        RegexOptions.IgnoreCase)]
    private static partial Regex LinkId();

    /// <summary>Returns the 11-character video id, or null if the text isn't a YouTube video.</summary>
    public static string? TryParseVideoId(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        if (BareId().IsMatch(trimmed))
            return trimmed;

        var match = LinkId().Match(trimmed);
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>Thumbnail URL for a video id (used as the library poster).</summary>
    public static string ThumbnailUrl(string videoId) =>
        $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

    /// <summary>Canonical watch URL, stored as the media item's path.</summary>
    public static string WatchUrl(string videoId) =>
        $"https://www.youtube.com/watch?v={videoId}";
}
