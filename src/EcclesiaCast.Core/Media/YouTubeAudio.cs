using System.Globalization;

namespace EcclesiaCast.Core.Media;

/// <summary>One video found by the YouTube search of the Música tab.</summary>
public sealed record YouTubeSearchResult(string VideoId, string Title, string Channel, TimeSpan? Duration)
{
    /// <summary>«4:05», «1:02:30», or «En vivo» for a broadcast with no end.</summary>
    public string DurationText => Duration is { } length
        ? (length.TotalHours >= 1 ? length.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                                  : length.ToString(@"m\:ss", CultureInfo.InvariantCulture))
        : "En vivo";

    public string ThumbnailUrl => $"https://i.ytimg.com/vi/{VideoId}/mqdefault.jpg";
}

/// <summary>
/// The pieces of the YouTube music of the Música tab that don't need the
/// program running: what yt-dlp prints for a search, which tracks of the list
/// are YouTube links, and what a pad button searches.
/// </summary>
public static class YouTubeAudio
{
    /// <summary>
    /// What yt-dlp is asked to print for each result, one line per video,
    /// with tabs in between (a title can have anything but a tab).
    /// </summary>
    public const string SearchPrintTemplate = "%(id)s\t%(title)s\t%(channel,uploader)s\t%(duration)s";

    /// <summary>The tones of the pad buttons, as a Spanish-speaking musician names them.</summary>
    public static IReadOnlyList<string> PadKeys { get; } =
        ["Do", "Do#", "Re", "Mib", "Mi", "Fa", "Fa#", "Sol", "Lab", "La", "Sib", "Si"];

    /// <summary>
    /// The search a pad button makes. YouTube's pads are mostly titled with
    /// the English letter («Worship Pad D»), so the tone is turned into it.
    /// </summary>
    public static string PadQuery(string key)
    {
        var letter = key.Trim() switch
        {
            "Do" => "C", "Do#" => "C#", "Reb" => "Db",
            "Re" => "D", "Re#" => "D#", "Mib" => "Eb",
            "Mi" => "E",
            "Fa" => "F", "Fa#" => "F#", "Solb" => "Gb",
            "Sol" => "G", "Sol#" => "G#", "Lab" => "Ab",
            "La" => "A", "La#" => "A#", "Sib" => "Bb",
            "Si" => "B",
            var other => other,
        };
        return $"worship pad {letter}";
    }

    /// <summary>A line printed with <see cref="SearchPrintTemplate"/>, or null for anything else (warnings, blanks).</summary>
    public static YouTubeSearchResult? ParseSearchLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var parts = line.Split('\t');
        if (parts.Length < 4)
            return null;

        var id = parts[0].Trim();
        if (YouTubeUrl.TryParseVideoId(id) != id)
            return null;

        var title = parts[1].Trim();
        if (title.Length == 0 || title == "NA")
            title = $"YouTube {id}";

        var channel = parts[2].Trim();
        if (channel == "NA")
            channel = string.Empty;

        TimeSpan? duration = double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                             && seconds > 0
            ? TimeSpan.FromSeconds(Math.Round(seconds))
            : null;

        return new YouTubeSearchResult(id, title, channel, duration);
    }

    /// <summary>
    /// The video id of a track of the list that is a YouTube link (played over
    /// the internet), or null for a file on this computer.
    /// </summary>
    public static string? TrackVideoId(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || !path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return null;

        return YouTubeUrl.TryParseVideoId(path);
    }

    /// <summary>
    /// True when a track of the list is this video: its YouTube link, or the
    /// file its sound was saved to (yt-dlp names it «title [id].m4a»).
    /// </summary>
    public static bool IsTrackOf(string? path, string videoId)
    {
        if (TrackVideoId(path) == videoId)
            return true;

        return path is not null
               && !path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
               && System.IO.Path.GetFileNameWithoutExtension(path).EndsWith($"[{videoId}]", StringComparison.Ordinal);
    }
}
