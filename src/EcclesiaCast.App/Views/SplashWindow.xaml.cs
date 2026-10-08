using System.Windows;
using System.Windows.Threading;
using EcclesiaCast.App.Services;

namespace EcclesiaCast.App.Views;

/// <summary>Shown while the program starts; closed once the main window is on screen.</summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = $"versión {AppInfo.Version}";
    }

    /// <summary>
    /// Shows the window and lets WPF paint it right now. Start-up keeps the UI
    /// thread busy afterwards, so without this the window would appear as an
    /// empty frame, or not until everything had loaded.
    /// </summary>
    public void ShowNow()
    {
        Show();
        Dispatcher.Invoke(DispatcherPriority.Render, () => { });
    }
}
