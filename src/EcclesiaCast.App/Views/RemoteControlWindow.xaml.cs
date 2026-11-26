using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using EcclesiaCast.Presentation.Remote;
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
    private readonly RemoteControlServer _server;
    private readonly Action<string> _pinChanged;

    public RemoteControlWindow(RemoteControlServer server, Action<string> pinChanged)
    {
        InitializeComponent();
        _server = server;
        _pinChanged = pinChanged;

        AddressBox.Text = server.Address;
        PinText.Text = server.Pin;
        NameNote.Text =
            "Si el celular deja de encontrar la computadora (el router le cambió la dirección), volvé a escanear este código. " +
            $"También podés probar con el nombre de la PC: {server.NameAddress} (suele funcionar en iPhone y en Android recientes).";
        ShowQr(server.Address);
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

    private void ChangePin_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = Notice.Confirm(
            "Se genera un PIN nuevo. Los celulares que ya estaban conectados van a pedir el PIN nuevo.\n\n" +
            "Las direcciones de OBS y vMix no se tocan: tienen su propio PIN.",
            yes: "Cambiar el PIN",
            title: "Cambiar el PIN del celular");
        if (!confirmed)
            return;

        var pin = _server.ChangePin();
        _pinChanged(pin);
        PinText.Text = pin;
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
