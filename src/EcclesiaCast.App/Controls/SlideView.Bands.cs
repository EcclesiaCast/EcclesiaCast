using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The bands of colour behind each line of the text (Temas → «Franjas»). WPF
/// does not say where a wrapped TextBlock broke its lines, so the box of every
/// letter is asked for and the boxes are gathered into lines. The bands live
/// in a canvas behind the text and outside its shadow, inside the text layer,
/// so they move and fade with the words.
/// </summary>
public partial class SlideView
{
    private bool _bandsHooked;

    /// <summary>Redraws the bands of both texts. Cheap when the theme has none.</summary>
    private void UpdateBands()
    {
        if (!_bandsHooked)
        {
            _bandsHooked = true;
            MainText.SizeChanged += (_, _) => UpdateBands();
            SecondaryText.SizeChanged += (_, _) => UpdateBands();
            TextLayer.SizeChanged += (_, _) => UpdateBands();
        }

        var theme = CurrentTheme;
        DrawBands(MainBands, MainText, theme);
        DrawBands(SecondaryBands, SecondaryText, theme);
    }

    private void DrawBands(Canvas canvas, TextBlock text, SlideTheme theme)
    {
        canvas.Children.Clear();
        if (theme.LineBand == LineBand.None
            || text.Visibility != Visibility.Visible
            || text.ActualWidth <= 0
            || text.Inlines.Count == 0)
            return;

        IReadOnlyList<BandBox> lines;
        try
        {
            lines = LineBandLayout.Lines(LetterBoxes(text, canvas));
        }
        catch (InvalidOperationException)
        {
            // Not laid out yet: SizeChanged comes back when it is.
            return;
        }

        // The edges of the screen, in the canvas's coordinates: the text layer
        // starts at its own margin from the left of the 1920 px canvas.
        var layerLeft = TextLayer.Margin.Left;
        var origin = canvas.TranslatePoint(new Point(0, 0), TextLayer);
        var screenLeft = -(origin.X + layerLeft);
        var screenRight = RootCanvas.Width - (origin.X + layerLeft);

        var brush = BrushFromHex(theme.LineBandColor, "#000000").Clone();
        brush.Opacity = Math.Clamp(theme.LineBandOpacity, 0, 1);
        brush.Freeze();

        foreach (var band in LineBandLayout.Bands(lines, theme.LineBand, text.FontSize, screenLeft, screenRight))
        {
            var rect = new Rectangle { Width = band.Width, Height = band.Height, Fill = brush };
            Canvas.SetLeft(rect, band.X);
            Canvas.SetTop(rect, band.Y);
            canvas.Children.Add(rect);
        }
    }

    /// <summary>The box of every visible letter of the text, in the canvas's coordinates.</summary>
    private static IEnumerable<BandBox> LetterBoxes(TextBlock text, Visual canvas)
    {
        var toCanvas = text.TransformToVisual(canvas);
        var pointer = text.ContentStart;
        var end = text.ContentEnd;

        while (pointer is not null && pointer.CompareTo(end) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                continue;
            }

            var run = pointer.GetTextInRun(LogicalDirection.Forward);
            for (var i = 0; i < run.Length; i++)
            {
                // Spaces are left out: the one where a line wraps would
                // stretch the band past the last word.
                if (char.IsWhiteSpace(run[i]))
                    continue;

                var start = pointer.GetPositionAtOffset(i, LogicalDirection.Forward);
                var after = pointer.GetPositionAtOffset(i + 1, LogicalDirection.Backward);
                if (start is null || after is null)
                    continue;

                var a = start.GetCharacterRect(LogicalDirection.Forward);
                var b = after.GetCharacterRect(LogicalDirection.Backward);
                if (a.IsEmpty || b.IsEmpty)
                    continue;

                var left = Math.Min(a.Left, b.Left);
                var right = Math.Max(a.Left, b.Left);
                var box = toCanvas.TransformBounds(new Rect(left, a.Top, Math.Max(0, right - left), a.Height));
                yield return new BandBox(box.X, box.Y, box.Width, box.Height);
            }

            pointer = pointer.GetPositionAtOffset(run.Length, LogicalDirection.Forward)
                      ?? pointer.GetNextContextPosition(LogicalDirection.Forward);
        }
    }
}
