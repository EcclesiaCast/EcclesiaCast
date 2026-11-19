using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace EcclesiaCast.App.Platform;

/// <summary>
/// Measures text with WPF's <see cref="FormattedText"/>, the same engine the
/// slide's <c>TextBlock</c>s draw with, for the shared fitting of the slide
/// text (<see cref="EcclesiaCast.Presentation.Rendering.SlideTextFit"/>).
/// </summary>
public sealed class WpfTextMetrics : ITextMetrics
{
    public static WpfTextMetrics Instance { get; } = new();

    // A whole chapter is measured each time one is opened: the typeface of a
    // theme is looked up once, not on every measure.
    private readonly ConcurrentDictionary<(string Family, int Weight, bool Italic), Typeface> _typefaces = new();

    public TextMeasure Measure(string text, string fontFamily, int weight, bool italic, double fontSize,
        double maxWidth, double? lineHeight, double pixelsPerDip)
    {
        var typeface = _typefaces.GetOrAdd((fontFamily, weight, italic), key => new Typeface(
            new FontFamily(key.Family),
            key.Italic ? FontStyles.Italic : FontStyles.Normal,
            FontWeight.FromOpenTypeWeight(key.Weight),
            FontStretches.Normal));

        var formatted = new FormattedText(
            text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, fontSize, Brushes.White, pixelsPerDip);

        if (!double.IsPositiveInfinity(maxWidth))
            formatted.MaxTextWidth = maxWidth;
        if (lineHeight is double height)
            formatted.LineHeight = height;

        return new TextMeasure(formatted.WidthIncludingTrailingWhitespace, formatted.Height);
    }
}
