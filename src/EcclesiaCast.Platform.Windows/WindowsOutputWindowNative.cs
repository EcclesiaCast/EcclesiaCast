using System.Runtime.InteropServices;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// The output and the stage windows on Windows: the calls the WPF windows
/// always made, moved here unchanged. <see cref="Prepare"/> adds
/// WS_EX_NOACTIVATE (the window never takes the keyboard);
/// <see cref="Place"/> stretches it over the screen with SetWindowPos, shown
/// and without activating it. Being «always on top» is the window's own
/// Topmost.
/// </summary>
public sealed class WindowsOutputWindowNative : IOutputWindowNative
{
    public void Prepare(NativeWindowHandle window)
    {
        var hwnd = window.Handle;
        _ = SetWindowLong(hwnd, GWL_EXSTYLE,
            GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
    }

    public void Place(NativeWindowHandle window, DisplayInfo display)
    {
        SetWindowPos(window.Handle, IntPtr.Zero,
            display.X, display.Y, display.Width, display.Height,
            SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
