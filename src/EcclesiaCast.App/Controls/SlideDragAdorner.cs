using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// What the operator sees while dragging a slide card: a copy of the card
/// under the mouse, and a bar in the gap between the two cards it will be
/// dropped between.
/// </summary>
public sealed class SlideDragAdorner : Adorner
{
    private readonly ImageSource _ghost;
    private readonly Size _ghostSize;
    private readonly Brush _barBrush;
    private Point? _pointer;
    private Rect? _bar;

    public SlideDragAdorner(UIElement adorned, FrameworkElement card, Brush barBrush) : base(adorned)
    {
        IsHitTestVisible = false;
        _barBrush = barBrush;

        // A picture of the card taken before it fades out in its place.
        var dpi = VisualTreeHelper.GetDpi(card);
        var width = Math.Max(1, card.ActualWidth);
        var height = Math.Max(1, card.ActualHeight);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(width * dpi.DpiScaleX), (int)Math.Ceiling(height * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(card);
        bitmap.Freeze();
        _ghost = bitmap;
        _ghostSize = new Size(width, height);
    }

    /// <summary>Moves the copy to the pointer and the bar to the gap (none outside the grid).</summary>
    public void Update(Point? pointer, Rect? bar)
    {
        _pointer = pointer;
        _bar = bar;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawing)
    {
        if (_bar is { } bar)
            drawing.DrawRoundedRectangle(_barBrush, null, bar, 2, 2);

        if (_pointer is { } pointer)
        {
            // A little smaller than the card, held by its upper part, so the
            // gap it is about to fill stays in sight.
            var size = new Size(_ghostSize.Width * 0.85, _ghostSize.Height * 0.85);
            var area = new Rect(new Point(pointer.X - size.Width / 2, pointer.Y - 18), size);
            drawing.PushOpacity(0.8);
            drawing.DrawImage(_ghost, area);
            drawing.Pop();
        }
    }
}
