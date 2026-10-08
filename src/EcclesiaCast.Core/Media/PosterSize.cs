namespace EcclesiaCast.Core.Media;

/// <summary>
/// The size of the poster kept for an image or a document page. It keeps the
/// picture's own shape instead of cutting it to 16:9, so the card of the
/// media bar can frame it exactly as the screen does — a tall page has to be
/// there whole to be shown or moved.
/// </summary>
public static class PosterSize
{
    /// <summary>The long side of a poster, in pixels (a 16:9 picture comes out 320×180, as it always did).</summary>
    public const int LongSide = 320;

    /// <summary>
    /// The size to draw a poster of a <paramref name="width"/> ×
    /// <paramref name="height"/> picture at: the long side is
    /// <paramref name="longSide"/>, the other follows the shape, and a picture
    /// that is already smaller is not blown up.
    /// </summary>
    public static (int Width, int Height) For(int width, int height, int longSide = LongSide)
    {
        if (width <= 0 || height <= 0)
            return (longSide, longSide);

        var scale = Math.Min(1.0, (double)longSide / Math.Max(width, height));
        return (
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));
    }
}
