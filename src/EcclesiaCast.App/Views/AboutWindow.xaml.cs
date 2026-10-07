using System.Windows;
using System.Windows.Navigation;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Who made the program and why, in the same dark style as the other small
/// windows (a plain MessageBox looked like a system error next to them).
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(string version)
    {
        InitializeComponent();
        VersionText.Text = $"versión {version}";
    }

    private void Link_Navigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // Sin navegador predeterminado no hay nada que hacer; el texto del
            // enlace queda a la vista para copiarlo.
        }

        e.Handled = true;
    }
}
