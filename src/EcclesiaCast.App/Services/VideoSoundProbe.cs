using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Asks VLC whether a video file carries sound, and how long it is. Used on
/// import: a video comes in silent (it is usually a background loop), except
/// a long one with sound — a testimony, an announcement — which is meant to
/// be heard.
/// </summary>
public static class VideoSoundProbe
{
    private const int ParseTimeoutMs = 4000;

    /// <summary>Null when VLC could not read the file.</summary>
    public static async Task<(bool HasSound, TimeSpan Duration)?> ProbeAsync(LibVLC libVlc, string path)
    {
        try
        {
            using var media = new Media(libVlc, new Uri(path));
            var status = await media.Parse(MediaParseOptions.ParseLocal, ParseTimeoutMs);
            if (status != MediaParsedStatus.Done)
                return null;

            var hasSound = media.Tracks.Any(t => t.TrackType == TrackType.Audio);
            return (hasSound, TimeSpan.FromMilliseconds(Math.Max(0, media.Duration)));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "VLC no pudo ver si {Path} trae sonido", path);
            return null;
        }
    }
}
