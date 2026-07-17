using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EcclesiaCast.App.Services;

/// <summary>Turns a window's title bar dark (Windows 10 2004+ / Windows 11).</summary>
public static class DarkTitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        var enabled = 1;
        try
        {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        }
        catch
        {
            // Older Windows without the attribute: keep the default title bar.
        }
    }
}
