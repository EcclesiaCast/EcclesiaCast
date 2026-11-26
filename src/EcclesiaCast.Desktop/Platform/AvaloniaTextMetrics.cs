using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// Measures text with Avalonia's <see cref="TextLayout"/>, the engine its
/// <c>TextBlock</c>s draw with, for the shared fitting of the slide text
/// (<see cref="SlideTextFit"/>). The font is the one really drawn on this
/// computer (<see cref="AvaloniaFontCatalog.Resolve"/>).
/// </summary>
public sealed class AvaloniaTextMetrics(AvaloniaFontCatalog fonts) : ITextMetrics
{
    public static AvaloniaTextMetrics Current => _current ??= new AvaloniaTextMetrics(AvaloniaFontCatalog.Current);

    private static AvaloniaTextMetrics? _current;

    /// <remarks>
    /// Avalonia lays text out in device-independent pixels whatever the
    /// screen, so <paramref name="pixelsPerDip"/> changes nothing here.
    /// </remarks>
    public TextMeasure Measure(string text, string fontFamily, int weight, bool italic, double fontSize,
        double maxWidth, double? lineHeight, double pixelsPerDip)
    {
        using var layout = Layout(text, fontFamily, weight, italic, fontSize, maxWidth, lineHeight);
        return new TextMeasure(layout.WidthIncludingTrailingWhitespace, layout.Height);
    }

    /// <summary>The lines as the layout cuts them, without the line breaks, for the tests and the benches.</summary>
    public IReadOnlyList<string> Lines(string text, string fontFamily, int weight, bool italic, double fontSize,
        double maxWidth, double? lineHeight)
    {
        using var layout = Layout(text, fontFamily, weight, italic, fontSize, maxWidth, lineHeight);
        return layout.TextLines
            .Select(line => text.Substring(line.FirstTextSourceIndex, Math.Min(line.Length, text.Length - line.FirstTextSourceIndex))
                .TrimEnd('\r', '\n').Trim())
            .ToList();
    }

    private TextLayout Layout(string text, string fontFamily, int weight, bool italic, double fontSize,
        double maxWidth, double? lineHeight)
    {
        var face = fonts.Face(fontFamily, weight, italic);
        return new(
            text,
            face.Typeface,
            fontSize,
            Brushes.White,
            textWrapping: double.IsPositiveInfinity(maxWidth) ? TextWrapping.NoWrap : TextWrapping.Wrap,
            maxWidth: maxWidth,
            lineHeight: lineHeight ?? double.NaN,
            letterSpacing: face.LetterSpacing(fontSize));
    }
}
