using System.Windows;
using System.Windows.Navigation;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Who made the program and why, in the same dark style as the other small
/// windows (a plain MessageBox looked like a system error next to them). The
/// very first time the program opens it is shown on its own, and then the
/// button says «Aceptar» instead of «Cerrar»: nobody asked to see it, so it
/// should ask for a go-ahead rather than look like something to dismiss.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(string version, bool firstRun = false)
    {
        InitializeComponent();
        VersionText.Text = $"versión {version}";
        if (Services.AppInfo.HasDonateUrl)
        {
            DonateLink.NavigateUri = new Uri(Services.AppInfo.DonateUrl);
            DonateLine.Visibility = Visibility.Visible;
        }
        if (firstRun)
            CloseButton.Content = "Aceptar";
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
