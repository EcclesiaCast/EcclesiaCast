using Avalonia.Controls;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// Opens «Ajustes»: not modal and only one, as in WPF. The operator's window
/// of Avalonia calls this for its menu (batch 19).
/// </summary>
public sealed class SettingsDialog(
    MainViewModel main, IPlatformInfo platform, UiModeChoice uiMode, IColorPicker colors, MenuDialogs menus)
{
    /// <summary>The one that is open, or null.</summary>
    internal SettingsWindow? Window { get; private set; }

    public void Show()
    {
        if (Window is { } open)
        {
            if (open.WindowState == WindowState.Minimized)
                open.WindowState = WindowState.Normal;
            open.Activate();
            return;
        }

        var owner = Modal.Owner;
        var window = new SettingsWindow(new SettingsViewModel(main, platform), uiMode, colors, menus)
        {
            WindowStartupLocation =
                owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
        };
        window.Closed += (_, _) => Window = null;
        Window = window;
        if (owner is null)
            window.Show();
        else
            window.Show(owner);
    }
}
