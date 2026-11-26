using Avalonia.Controls;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Desktop.Platform;

/// <summary>
/// Which screen holds the operator's window: the one Avalonia says the
/// window is on, compared by its place and size. Before the window is given
/// (and on a system that cannot tell) the main screen is taken, so a single
/// screen still counts as «covers the operator».
/// </summary>
public sealed class AvaloniaOperatorScreen : IOperatorScreen
{
    public Window? Window { get; set; }

    public bool Holds(DisplayInfo display)
    {
        var screen = Window is { Screens: { } screens } window ? screens.ScreenFromWindow(window) : null;
        if (screen is null)
            return display.IsPrimary;

        return screen.Bounds.X == display.X && screen.Bounds.Y == display.Y
            && screen.Bounds.Width == display.Width && screen.Bounds.Height == display.Height;
    }
}
