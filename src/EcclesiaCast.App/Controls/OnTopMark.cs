using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using EcclesiaCast.Core.Media;
using Path = System.Windows.Shapes.Path;

namespace EcclesiaCast.App.Controls;

/// <summary>The «goes on top of everything» icon (<see cref="OnTopIcon"/>) for the places built from code.</summary>
public static class OnTopMark
{
    public static Path Create(double size = 12) => new()
    {
        Data = Geometry.Parse(OnTopIcon.PathData),
        Fill = (Brush?)Application.Current.TryFindResource("OnTopBrush") ?? Brushes.Goldenrod,
        Stretch = Stretch.Uniform,
        Width = size,
        Height = size,
        VerticalAlignment = VerticalAlignment.Center,
        ToolTip = OnTopIcon.Explanation,
    };

    /// <summary>The same icon in the middle of a sentence.</summary>
    public static Inline InText(double size = 12) =>
        new InlineUIContainer(Create(size)) { BaselineAlignment = BaselineAlignment.Center };
}
