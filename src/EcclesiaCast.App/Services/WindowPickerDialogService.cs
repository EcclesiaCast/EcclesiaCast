using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Capture;

namespace EcclesiaCast.App.Services;

public sealed class WindowPickerDialogService : IWindowPickerDialog
{
    public WindowPick? Pick(IWindowCapture capture, bool showCursor)
    {
        var window = new WindowPickerWindow(capture, showCursor)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true
            ? new WindowPick(window.Chosen, window.UseWindowsPicker, window.ShowCursor, window.SaveToMedia)
            : null;
    }
}
