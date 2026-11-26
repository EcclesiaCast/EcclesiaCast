using Avalonia.Controls;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.Services;
using Serilog;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// Who made the program and why. The very first time the program opens it is
/// shown on its own, and then the button says «Aceptar» instead of «Cerrar»:
/// nobody asked to see it, so it should ask for a go-ahead rather than look
/// like something to dismiss.
/// </summary>
public sealed partial class AboutWindow : Window
{
    public const string GitHubUrl = "https://github.com/EcclesiaCast";

    private readonly IShell _shell;

    public AboutWindow(IShell shell, string version, bool firstRun = false)
    {
        InitializeComponent();
        _shell = shell;
        VersionText.Text = $"versión {version}";
        DonateLine.IsVisible = AppInfo.HasDonateUrl;
        if (firstRun)
            CloseButton.Content = "Aceptar";
    }

    private void GitHub_Click(object? sender, RoutedEventArgs e) => OpenLink(GitHubUrl);

    private void Donate_Click(object? sender, RoutedEventArgs e) => OpenLink(AppInfo.DonateUrl);

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void OpenLink(string url)
    {
        try
        {
            _shell.OpenUrl(url);
        }
        catch (Exception ex)
        {
            // Sin navegador predeterminado no hay nada que hacer; el texto del
            // enlace queda a la vista.
            Log.Warning(ex, "No se pudo abrir {Url}", url);
        }
    }
}
