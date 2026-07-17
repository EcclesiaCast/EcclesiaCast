using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the app-themed color picker and returns a hex string.</summary>
public static class ColorPickerHelper
{
    public static string? Pick(string? currentHex)
    {
        var window = new ColorPickerWindow(currentHex)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.ResultHex : null;
    }
}
