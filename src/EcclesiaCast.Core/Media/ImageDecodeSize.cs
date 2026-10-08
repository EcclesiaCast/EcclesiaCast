namespace EcclesiaCast.Core.Media;

/// <summary>
/// How wide to read an image so it looks sharp on the screen once the
/// framing has zoomed it in. Reading every picture at the width of the screen
/// is right for a picture shown whole, but a zoomed one just enlarges that
/// small copy and goes soft — which is what a page of a PDF did at 160 %.
/// </summary>
public static class ImageDecodeSize
{
    /// <summary>What every image has always been read at: the width of the 1920×1080 canvas.</summary>
    public const int Baseline = 1920;

    /// <summary>Never read wider than this: a 4096×2304 picture is already 36 MB in memory.</summary>
    public const int Max = 4096;

    private const int Min = 256;
    private const double CanvasWidth = 1920;
    private const double CanvasHeight = 1080;

    /// <summary>
    /// True when the file's own size is needed to decide. Without zoom the
    /// answer is always <see cref="Baseline"/>, so the caller can skip opening
    /// the file for it: showing an image costs what it always cost.
    /// </summary>
    public static bool NeedsFileSize(double zoom) => zoom > 1.001;

    /// <summary>
    /// The width to read an image at for a media framed with
    /// <paramref name="scaling"/>, <paramref name="zoom"/> and, if it has one,
    /// a fixed size on the canvas. Never wider than the file itself or
    /// <see cref="Max"/>.
    /// </summary>
    public static int For(
        MediaScaling scaling, double zoom, int? frameWidth, int? frameHeight, int fileWidth, int fileHeight)
    {
        if (!NeedsFileSize(zoom) || fileWidth <= 0 || fileHeight <= 0)
            return Baseline;

        var boxWidth = frameWidth is > 0 ? Math.Min(frameWidth.Value, CanvasWidth) : CanvasWidth;
        var boxHeight = frameHeight is > 0 ? Math.Min(frameHeight.Value, CanvasHeight) : CanvasHeight;
        var aspect = (double)fileWidth / fileHeight;

        // How wide the picture ends up on the canvas before the zoom: Fit
        // keeps all of it inside the box, Fill covers the box (a portrait page
        // is as wide as the box, a panorama is wider), Stretch is the box.
        var shown = scaling switch
        {
            MediaScaling.Fit => Math.Min(boxWidth, boxHeight * aspect),
            MediaScaling.Stretch => boxWidth,
            _ => Math.Max(boxWidth, boxHeight * aspect),
        };

        var needed = (int)Math.Ceiling(shown * Math.Min(zoom, 4));
        return Math.Min(Math.Min(fileWidth, Max), Math.Max(Min, needed));
    }
}
