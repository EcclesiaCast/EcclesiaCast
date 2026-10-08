namespace EcclesiaCast.Core.Media;

/// <summary>A rectangle in the units of the screen it is placed on.</summary>
public readonly record struct FramedRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
}

/// <summary>
/// Where «Rellenar» puts a picture: the numbers for a picture drawn WHOLE and
/// then scaled and moved, instead of letting the toolkit crop it first.
///
/// WPF's UniformToFill crops the overflow with a layout clip and the zoom and
/// shift are applied afterwards to the cropped piece, so what was cropped
/// could never be shown again (a tall page could not be moved down to its
/// bottom, and zooming out left a shrunken crop in the black). Here the
/// picture is drawn as «Ajustar» would (everything inside the box, centred)
/// and the rest is a scale and a shift of that whole picture.
///
/// The covering picture is centred on its box, so what is cut is cut evenly
/// from both sides (WPF's UniformToFill anchored it at the top-left corner,
/// showing the top of a tall photo and the left of a wide one).
/// </summary>
public static class FillFraming
{
    /// <summary>The canvas the fixed sizes of a media are expressed over.</summary>
    public const double CanvasWidth = 1920;
    public const double CanvasHeight = 1080;

    /// <summary>
    /// What to do to the picture drawn whole in the middle of its box.
    /// </summary>
    /// <param name="CoverScale">How much bigger the covering picture is than the one that fits whole (1 when they are the same).</param>
    /// <param name="Scale">Scale to apply around the centre of the drawn picture: <paramref name="CoverScale"/> times the zoom.</param>
    /// <param name="ShiftX">Then move it by this much (same units as the container).</param>
    /// <param name="ShiftY">Then move it by this much.</param>
    /// <param name="Window">With a fixed size: the box, as a rectangle in the drawn picture's own coordinates, so that the picture's clip keeps it still on screen. Null without a fixed size.</param>
    public readonly record struct Placement(
        double CoverScale, double Scale, double ShiftX, double ShiftY, FramedRect? Window);

    /// <summary>The size of the box the picture goes in: the whole container, or the fixed size scaled to it.</summary>
    public static (double Width, double Height) BoxSize(
        double containerWidth, double containerHeight, int? frameWidth, int? frameHeight)
    {
        if (frameWidth is not > 0 || frameHeight is not > 0 || containerWidth <= 0 || containerHeight <= 0)
            return (containerWidth, containerHeight);

        return (
            Math.Min(containerWidth, frameWidth.Value * containerWidth / CanvasWidth),
            Math.Min(containerHeight, frameHeight.Value * containerHeight / CanvasHeight));
    }

    /// <summary>
    /// The placement of a picture of <paramref name="sourceWidth"/> ×
    /// <paramref name="sourceHeight"/> in a box of <paramref name="boxWidth"/> ×
    /// <paramref name="boxHeight"/> that sits in the middle of the container.
    /// Zoom turns around the middle of the container; the shifts are in the
    /// container's units.
    /// </summary>
    public static Placement Place(
        double sourceWidth, double sourceHeight,
        double containerWidth, double containerHeight,
        double boxWidth, double boxHeight, bool hasFrame,
        double zoom, double shiftX, double shiftY)
    {
        var fit = Math.Min(boxWidth / sourceWidth, boxHeight / sourceHeight);
        var cover = Math.Max(boxWidth / sourceWidth, boxHeight / sourceHeight);
        var coverScale = cover / fit;

        // The drawn picture's middle is the container's middle, and so is the
        // covering picture's: only the shift moves it.
        var scale = coverScale * zoom;
        var moveX = shiftX;
        var moveY = shiftY;

        FramedRect? window = null;
        if (hasFrame)
        {
            // The box on screen is fixed; mapped back through the scale and
            // the move into the drawn picture's own coordinates.
            window = new FramedRect(
                sourceWidth * fit / 2 + (-boxWidth / 2 - moveX) / scale,
                sourceHeight * fit / 2 + (-boxHeight / 2 - moveY) / scale,
                boxWidth / scale,
                boxHeight / scale);
        }

        return new Placement(coverScale, scale, moveX, moveY, window);
    }

    /// <summary>
    /// The rectangle the covering picture takes with no zoom and no shift, in
    /// the container's units. It reaches past the container only when the
    /// picture is not the shape of the box.
    /// </summary>
    public static FramedRect NaturalBounds(
        double sourceWidth, double sourceHeight,
        double containerWidth, double containerHeight,
        double boxWidth, double boxHeight)
    {
        var cover = Math.Max(boxWidth / sourceWidth, boxHeight / sourceHeight);
        return new FramedRect(
            (containerWidth - sourceWidth * cover) / 2,
            (containerHeight - sourceHeight * cover) / 2,
            sourceWidth * cover,
            sourceHeight * cover);
    }
}
