using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.App.Services;

/// <summary>What the operator chose in the «Proyectar otra ventana» list.</summary>
/// <param name="Window">The window picked, or null when Windows' own picker is to be used.</param>
/// <param name="UseWindowsPicker">Open Windows' picker (windows and whole screens) instead.</param>
/// <param name="ShowCursor">Draw the mouse pointer on the captured picture.</param>
public sealed record WindowPick(RawWindow? Window, bool UseWindowsPicker, bool ShowCursor);

/// <summary>Opens the list of windows that can be projected.</summary>
public interface IWindowPickerDialog
{
    /// <summary>Shows it modally; null when the operator closed it without choosing.</summary>
    WindowPick? Pick(IWindowCapture capture, bool showCursor);
}

public sealed class WindowPickerDialogService : IWindowPickerDialog
{
    public WindowPick? Pick(IWindowCapture capture, bool showCursor)
    {
        var window = new WindowPickerWindow(capture, showCursor)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? new WindowPick(window.Chosen, window.UseWindowsPicker, window.ShowCursor)
            : null;
    }
}
