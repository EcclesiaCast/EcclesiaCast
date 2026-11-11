using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

/// <summary>Opens the app-themed color picker and returns a hex string.</summary>
public static class ColorPickerHelper
{
    /// <summary>Sentinel returned when the operator picks "Transparent".</summary>
    public const string Transparent = ColorPickerWindow.Transparent;

    /// <summary>Where the colors used lately are kept; assigned at start-up (App.xaml.cs).</summary>
    public static EcclesiaCast.Core.Abstractions.ISettingsStore? Settings { get; set; }

    public static string? Pick(string? currentHex, bool allowTransparent = false)
    {
        var window = new ColorPickerWindow(currentHex, allowTransparent)
        {
            // Over the window that asked (the theme editor, a designer), not
            // the main one: otherwise it opens centred somewhere else.
            Owner = System.Windows.Application.Current.Windows
                        .OfType<System.Windows.Window>()
                        .FirstOrDefault(w => w.IsActive)
                    ?? System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.ResultHex : null;
    }
}
