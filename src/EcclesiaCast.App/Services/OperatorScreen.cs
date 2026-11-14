using System.Windows;
using System.Windows.Interop;
using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Which screen holds the operator panel. A fullscreen window sent there
/// covers the panel it was started from: with a single screen (a notebook, or
/// a projector in «duplicate» mode) that is the only choice there is.
/// </summary>
public static class OperatorScreen
{
    /// <summary>True when that display is the one holding the operator window.</summary>
    public static bool Holds(DisplayInfo display)
    {
        var window = Application.Current?.MainWindow;
        if (window is null)
            return false;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return false;

        var screen = System.Windows.Forms.Screen.FromHandle(handle);
        return string.Equals(screen.DeviceName, display.DeviceName, StringComparison.Ordinal);
    }
}
