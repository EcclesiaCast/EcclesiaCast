using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// The zoom and the shift of a media as the screen applies them: held within
/// their limits, and the shift turned from screens into the units of the
/// container. The framing of WPF and of Avalonia both start from here and
/// then place the picture with <see cref="FillFraming"/>.
/// </summary>
public static class FramingNumbers
{
    public const double MinZoom = 0.1;
    public const double MaxZoom = 4;

    /// <summary>The zoom, between <see cref="MinZoom"/> and <see cref="MaxZoom"/>; 1 when none was set.</summary>
    public static double Zoom(MediaItem media) =>
        Math.Clamp(media.Zoom <= 0 ? 1 : media.Zoom, MinZoom, MaxZoom);

    /// <summary>The shift, in the units of a container of that size.</summary>
    public static (double X, double Y) Offset(MediaItem media, double containerWidth, double containerHeight) =>
        (Math.Clamp(media.OffsetX, -FillFraming.MaxOffset, FillFraming.MaxOffset) * containerWidth,
         Math.Clamp(media.OffsetY, -FillFraming.MaxOffset, FillFraming.MaxOffset) * containerHeight);
}
