using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using EcclesiaCast.Core.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>The «goes on top of everything» icon (<see cref="OnTopIcon"/>) for the places built from code.</summary>
public static class OnTopMark
{
    public static Path Create(double size = 12)
    {
        var mark = new Path
        {
            Data = Geometry.Parse(OnTopIcon.PathData),
            Fill = Application.Current?.TryFindResource("OnTopBrush", out var brush) == true
                ? (IBrush?)brush
                : Brushes.Goldenrod,
            Stretch = Stretch.Uniform,
            Width = size,
            Height = size,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(mark, OnTopIcon.Explanation);
        return mark;
    }

    /// <summary>The same icon in the middle of a sentence.</summary>
    public static Inline InText(double size = 12) =>
        new InlineUIContainer(Create(size)) { BaselineAlignment = BaselineAlignment.Center };
}
