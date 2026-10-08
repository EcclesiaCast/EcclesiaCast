namespace EcclesiaCast.Core.Displays;

/// <summary>
/// The size of the LIVE and PREVIEW boxes. They show what the output screen
/// shows, so they keep its proportion (16:9, say) always. A wider panel does
/// not make the box taller: it stays at the height it had, centred, with room
/// at the sides; a narrower panel shrinks the whole box.
/// </summary>
public static class AspectFit
{
    /// <summary>The proportion assumed when no output screen is known.</summary>
    public const double DefaultRatio = 16d / 9d;

    /// <summary>Width over height of a screen, or <see cref="DefaultRatio"/> for nonsense values.</summary>
    public static double RatioOf(int width, int height) =>
        width > 0 && height > 0 ? (double)width / height : DefaultRatio;

    /// <param name="availableWidth">Width the panel gives (infinite when unconstrained).</param>
    /// <param name="maxHeight">The height the box never exceeds.</param>
    /// <param name="ratio">Width over height of the output.</param>
    public static (double Width, double Height) Compute(double availableWidth, double maxHeight, double ratio)
    {
        if (!(ratio > 0) || double.IsNaN(ratio) || double.IsInfinity(ratio))
            ratio = DefaultRatio;

        var width = Math.Min(Math.Max(0, availableWidth), maxHeight * ratio);
        return (width, width / ratio);
    }
}
