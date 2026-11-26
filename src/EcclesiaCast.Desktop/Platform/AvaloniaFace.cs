using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// A face of a font as the projected text draws it
/// (<see cref="AvaloniaFontCatalog.Face"/>). When WPF would thicken the face
/// (Impact in bold), its letters are also as much wider as WPF makes them.
/// </summary>
public sealed record AvaloniaFace(FontFamily Family, FontWeight Weight, FontStyle Style, FontStretch Stretch,
    bool SimulatedBold)
{
    public Typeface Typeface => new(Family, Style, Weight, Stretch);

    /// <summary>
    /// The room added after each letter. WPF adds it to the letters only;
    /// Avalonia adds it to the spaces too, so a thickened line is 2 % of the
    /// size per space wider than in WPF.
    /// </summary>
    public double LetterSpacing(double fontSize) =>
        SimulatedBold ? fontSize * FontFaceChoice.SimulatedBoldAdvance : 0;

    /// <summary>Gives <paramref name="text"/> this face, and keeps its letter spacing right when its size changes.</summary>
    public void ApplyTo(TextBlock text)
    {
        text.FontFamily = Family;
        text.FontWeight = Weight;
        text.FontStyle = Style;
        text.FontStretch = Stretch;
        TextFaces.SetSimulatedBold(text, SimulatedBold);
    }
}

/// <summary>Keeps the letter spacing of a thickened text in step with its size.</summary>
public static class TextFaces
{
    private static readonly AttachedProperty<bool> SimulatedBoldProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("SimulatedBold", typeof(TextFaces));

    static TextFaces()
    {
        TextBlock.FontSizeProperty.Changed.AddClassHandler<TextBlock>((text, _) => Space(text));
        SimulatedBoldProperty.Changed.AddClassHandler<TextBlock>((text, _) => Space(text));
    }

    public static void SetSimulatedBold(TextBlock text, bool value) => text.SetValue(SimulatedBoldProperty, value);

    private static void Space(TextBlock text)
    {
        if (!text.IsSet(SimulatedBoldProperty))
            return;
        text.LetterSpacing = text.GetValue(SimulatedBoldProperty)
            ? text.FontSize * FontFaceChoice.SimulatedBoldAdvance
            : 0;
    }
}
