using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Holds the LIVE and PREVIEW pictures at the proportion of the output
/// screen. The box is as wide as the panel allows up to the width that makes it
/// <see cref="MaxBoxHeight"/> tall, and sits centred: widening the panel leaves
/// room at the sides instead of stretching the picture or growing it downward.
/// </summary>
public sealed class AspectFitBox : Decorator
{
    public static readonly DependencyProperty AspectRatioProperty =
        DependencyProperty.Register(nameof(AspectRatio), typeof(double), typeof(AspectFitBox),
            new FrameworkPropertyMetadata(AspectFit.DefaultRatio,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty MaxBoxHeightProperty =
        DependencyProperty.Register(nameof(MaxBoxHeight), typeof(double), typeof(AspectFitBox),
            new FrameworkPropertyMetadata(165d,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>Width over height of the output screen.</summary>
    public double AspectRatio
    {
        get => (double)GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    /// <summary>The height the box never goes beyond.</summary>
    public double MaxBoxHeight
    {
        get => (double)GetValue(MaxBoxHeightProperty);
        set => SetValue(MaxBoxHeightProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var (width, height) = AspectFit.Compute(constraint.Width, MaxBoxHeight, AspectRatio);
        Child?.Measure(new Size(width, height));

        // Takes the whole width the panel offers, so the box can be centred in it.
        return new Size(double.IsInfinity(constraint.Width) ? width : constraint.Width, height);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var (width, height) = AspectFit.Compute(arrangeSize.Width, MaxBoxHeight, AspectRatio);
        Child?.Arrange(new Rect((arrangeSize.Width - width) / 2, 0, width, height));
        return arrangeSize;
    }
}
