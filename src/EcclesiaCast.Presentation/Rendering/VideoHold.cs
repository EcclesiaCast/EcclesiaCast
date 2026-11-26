namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// The last picture of a video, frozen on screen while whatever replaces it
/// arrives (the first frame of the next video, or an image fading in over
/// it): changing media never goes through black. If the replacement never
/// comes (VLC could not open the file), the picture is let go anyway.
/// </summary>
public static class VideoHold
{
    /// <summary>
    /// How long a held picture waits for its replacement: two seconds, or the
    /// fade and a margin if that is longer. It has to outlast the fade of what
    /// replaces it, or what is underneath shows before the new one is drawn.
    /// </summary>
    public static TimeSpan Timeout(TimeSpan fade) =>
        TimeSpan.FromSeconds(Math.Max(2, fade.TotalSeconds + 1));
}
