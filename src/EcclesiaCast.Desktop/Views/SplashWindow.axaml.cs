using Avalonia.Controls;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Views;

/// <summary>Shown while the program starts; closed once the main window is on screen.</summary>
public sealed partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = $"versión {AppInfo.Version}";
    }
}
