using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// A glowing frame around one element of a window that a tutorial step opened
/// (the lyrics box of the song editor, the QR of the phone window): the tour's
/// own overlay only covers the main window, so the other windows get this.
/// </summary>
public sealed class TourPulse : Adorner
{
    private readonly Pen _pen;
    private readonly AdornerLayer _layer;

    private TourPulse(UIElement target, AdornerLayer layer)
        : base(target)
    {
        _layer = layer;
        IsHitTestVisible = false;
        var accent = (Brush?)Application.Current.TryFindResource("AccentSoftBrush") ?? Brushes.DeepSkyBlue;
        _pen = new Pen(accent, 3);
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.45, TimeSpan.FromMilliseconds(700))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
        });
    }

    /// <summary>Frames <paramref name="element"/>; null if its window has no adorner layer yet.</summary>
    public static TourPulse? Show(FrameworkElement element)
    {
        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer is null)
            return null;

        var pulse = new TourPulse(element, layer);
        layer.Add(pulse);
        return pulse;
    }

    public void Remove()
    {
        BeginAnimation(OpacityProperty, null);
        _layer.Remove(this);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var rect = new Rect(AdornedElement.RenderSize);
        rect.Inflate(4, 4);
        drawingContext.DrawRoundedRectangle(null, _pen, rect, 6, 6);
    }
}
