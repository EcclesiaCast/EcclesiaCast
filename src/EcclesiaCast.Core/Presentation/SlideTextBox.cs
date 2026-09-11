using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// An extra block of text sitting on a slide, beside the words themselves:
/// the name of the series in a corner, a reference under the lyric, a line in
/// another language next to it.
///
/// Positions are in pixels over the same virtual 1920×1080 canvas the rest of
/// the slide uses, so a design looks identical in the operator's preview and
/// on the projector.
/// </summary>
public sealed record SlideTextBox(
    string Text,
    double X,
    double Y,
    double Width,
    double Height,
    double FontSize = 48,
    string? FontFamily = null,
    string Color = "#FFFFFF",
    bool Bold = false,
    bool Italic = false,
    HAlign AlignH = HAlign.Center,
    VAlign AlignV = VAlign.Center)
{
    /// <summary>A new box low on the canvas, out of the way of centred lyrics.</summary>
    public static SlideTextBox Fresh(string text = "Texto") =>
        new(text, X: 660, Y: 820, Width: 600, Height: 140);
}
