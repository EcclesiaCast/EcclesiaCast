namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// Avalonia does not notice by itself that the pixels of a bitmap changed:
/// the small copy of the projector's video (<c>ProjectionViewModel.LiveFrame</c>)
/// and the frames of a video logo (<c>LiveLogoFrame</c>) are written in
/// place, frame after frame. The output window says so here on every frame,
/// and every view that shows those copies (the operator's live picture, the
/// stage mirror) asks to be drawn again.
/// </summary>
public static class LiveFrames
{
    /// <summary>New pixels in the small copy of the background video.</summary>
    public static event EventHandler? VideoWritten;

    /// <summary>New pixels in the frame of the video logo.</summary>
    public static event EventHandler? LogoWritten;

    public static void RaiseVideoWritten() => VideoWritten?.Invoke(null, EventArgs.Empty);

    public static void RaiseLogoWritten() => LogoWritten?.Invoke(null, EventArgs.Empty);
}
