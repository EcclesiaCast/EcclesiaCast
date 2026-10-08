using System.Globalization;

namespace EcclesiaCast.Core.Media;

/// <summary>
/// Reads what ffmpeg and ffprobe print, to draw a progress bar for a copy
/// that takes minutes. Kept apart from the process handling so the parsing can
/// be tested with the exact lines the programs write.
/// </summary>
public static class FfmpegProgress
{
    /// <summary>
    /// ffmpeg run with <c>-progress pipe:1</c> writes a block of "key=value"
    /// lines every half second. <c>out_time_us</c> is how much of the video is
    /// done, in microseconds; older builds call the same number <c>out_time_ms</c>
    /// (it is microseconds there too, a long-standing misnomer).
    /// </summary>
    public static bool TryParseTime(string line, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        var trimmed = line.Trim();

        string? value = null;
        foreach (var key in new[] { "out_time_us=", "out_time_ms=" })
        {
            if (trimmed.StartsWith(key, StringComparison.Ordinal))
            {
                value = trimmed[key.Length..];
                break;
            }
        }

        // "N/A" while the first frame is still being prepared.
        if (value is null || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var micro) || micro < 0)
            return false;

        time = TimeSpan.FromTicks(micro * 10);
        return true;
    }

    /// <summary>The length of a video from <c>ffprobe -show_entries format=duration -of csv=p=0</c> ("12.345000").</summary>
    public static bool TryParseDuration(string output, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        var line = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (line is null
            || !double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || seconds <= 0 || double.IsInfinity(seconds))
            return false;

        duration = TimeSpan.FromSeconds(seconds);
        return true;
    }

    /// <summary>How far along the copy is, from 0 to 1. Zero while the length is unknown.</summary>
    public static double Fraction(TimeSpan done, TimeSpan total) =>
        total <= TimeSpan.Zero ? 0 : Math.Clamp(done.TotalSeconds / total.TotalSeconds, 0, 1);
}
