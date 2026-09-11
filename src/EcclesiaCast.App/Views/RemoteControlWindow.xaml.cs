using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using EcclesiaCast.App.Remote;
using QRCoder;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Shows how to reach the phone remote: a QR to scan, the address to type by
/// hand, and the PIN. Nothing here is secret beyond the PIN — the server only
/// listens on the local network.
/// </summary>
public partial class RemoteControlWindow : Window
{
    public RemoteControlWindow(RemoteControlServer server)
    {
        InitializeComponent();

        AddressBox.Text = server.Address;
        PinText.Text = server.Pin;
        BroadcastBox.Text = server.BroadcastAddress;
        ShowQr(server.Address);
    }

    private void CopyBroadcast_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BroadcastBox.Text);
        }
        catch (Exception ex)
        {
            // El portapapeles lo puede tener tomado otro programa.
            Log.Debug(ex, "No se pudo copiar la dirección de transmisión");
        }
    }

    /// <summary>True when the operator asked to switch the remote off.</summary>
    public bool StopRequested { get; private set; }

    private void ShowQr(string address)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(address, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(10);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(png);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            QrImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            // Without the code the address is still typeable, so this is not fatal.
            Log.Warning(ex, "No se pudo generar el código QR del control remoto");
            QrImage.Visibility = Visibility.Collapsed;
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopRequested = true;
        Close();
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
