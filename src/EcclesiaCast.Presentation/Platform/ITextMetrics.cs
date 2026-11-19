namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Measures one block of text with the text engine of the interface that
/// draws it (WPF, Avalonia). It is the only thing the fitting of the slide
/// text (<see cref="Rendering.SlideTextFit"/>) asks of the interface: the sum
/// itself is shared, so both cut the lines and choose the size the same way.
/// </summary>
public interface ITextMetrics
{
    /// <summary>
    /// The size the text takes.
    /// </summary>
    /// <param name="text">The text; a line break written in it starts a new line.</param>
    /// <param name="fontFamily">The name of the font family, as the theme says it.</param>
    /// <param name="weight">The OpenType weight: 400 normal, 600 semibold, 700 bold.</param>
    /// <param name="italic">True for italics.</param>
    /// <param name="fontSize">The size of the letters, in device-independent pixels.</param>
    /// <param name="maxWidth">Where the lines are cut; <see cref="double.PositiveInfinity"/> to not cut them.</param>
    /// <param name="lineHeight">The height of each line; null for the font's own.</param>
    /// <param name="pixelsPerDip">The pixels of the screen per device-independent pixel.</param>
    TextMeasure Measure(string text, string fontFamily, int weight, bool italic, double fontSize,
        double maxWidth, double? lineHeight, double pixelsPerDip);
}

/// <summary>
/// The size of a measured block of text. <see cref="Width"/> counts the spaces
/// at the end of a line, as the screen keeps room for them.
/// </summary>
public readonly record struct TextMeasure(double Width, double Height);
