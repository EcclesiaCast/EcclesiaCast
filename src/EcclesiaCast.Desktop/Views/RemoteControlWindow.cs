using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EcclesiaCast.Presentation.Remote;
using EcclesiaCast.Presentation.Services;
using QRCoder;
using Serilog;

namespace EcclesiaCast.Desktop.Views;

/// <summary>
/// How to reach the phone remote, in Avalonia (the WPF
/// <c>RemoteControlWindow</c>): a QR to scan, the address to type by hand and
/// the PIN. Nothing here is secret beyond the PIN: the server only listens on
/// the local network.
/// </summary>
public sealed class RemoteControlWindow : Window
{
    public RemoteControlWindow(RemoteControlServer server)
    {
        Title = "Control desde el celular";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;

        var body = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
        body.Children.Add(new TextBlock { Text = "Control desde el celular", FontSize = 18, FontWeight = FontWeight.SemiBold });
        body.Children.Add(Wrapped("El celular tiene que estar en la misma red wifi que esta computadora."));
        body.Children.Add(Wrapped("Escaneá el código, o escribí esta dirección en el navegador del celular:"));

        if (Qr(server.Address) is { } qr)
            body.Children.Add(new Image { Source = qr, Width = 220, Height = 220, HorizontalAlignment = HorizontalAlignment.Center });

        body.Children.Add(new SelectableTextBlock { Text = server.Address, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center });
        body.Children.Add(new TextBlock { Text = $"PIN: {server.Pin}", FontSize = 22, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        body.Children.Add(Wrapped("El PIN se pide una sola vez en cada celular."));
        body.Children.Add(Wrapped(
            "La primera vez, la Mac puede preguntar si permitís que EcclesiaCast reciba conexiones: tocá «Permitir»."));

        var stop = new Button { Content = "Apagar el control" };
        stop.Click += (_, _) =>
        {
            StopRequested = true;
            Close();
        };
        var done = new Button { Content = "Listo", IsDefault = true, IsCancel = true };
        done.Click += (_, _) => Close();
        body.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { stop, done },
        });
        Content = body;
    }

    /// <summary>True when the operator asked to switch the remote off.</summary>
    public bool StopRequested { get; private set; }

    private static TextBlock Wrapped(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    private static Bitmap? Qr(string address)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(address, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(10);
            using var stream = new MemoryStream(png);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            // Without the code the address is still typeable, so this is not fatal.
            Log.Warning(ex, "No se pudo generar el código QR del control remoto");
            return null;
        }
    }
}

/// <summary>The phone remote's window, waiting for the operator like the WPF one.</summary>
public sealed class RemoteControlDialog : IRemoteControlDialog
{
    public bool Show(RemoteControlServer server)
    {
        var window = new RemoteControlWindow(server);
        Modal.Run(window);
        return window.StopRequested;
    }
}
