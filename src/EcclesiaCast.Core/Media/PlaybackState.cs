namespace EcclesiaCast.Core.Media;

/// <summary>
/// Where the projected video is right now, for the operator's transport bar.
/// <see cref="Duration"/> is zero while the player is still opening the file
/// (and for still images, which have no transport at all).
/// </summary>
public readonly record struct PlaybackState(
    bool HasVideo,
    bool IsPlaying,
    TimeSpan Position,
    TimeSpan Duration)
{
    public static PlaybackState None { get; } = new(false, false, TimeSpan.Zero, TimeSpan.Zero);

    /// <summary>True when the video reported a length, so seeking makes sense.</summary>
    public bool CanSeek => HasVideo && Duration > TimeSpan.Zero;
}
