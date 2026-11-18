using System.Windows;
using EcclesiaCast.Presentation.Remote;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Remote;
using Serilog;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «Transmisión → Salida para OBS o vMix…»: the addresses of the stream's
/// layers, to paste into OBS or vMix.
/// </summary>
public partial class BroadcastWindow : Window
{
    private readonly RemoteControlServer _server;
    private readonly Action<Window> _editStyles;

    public BroadcastWindow(RemoteControlServer server, Action<Window> editStyles)
    {
        InitializeComponent();
        _server = server;
        _editStyles = editStyles;
        BroadcastBox.Text = server.BroadcastAddress;
    }

    /// <summary>Copies the address of the page of one channel of the stream (the button names it).</summary>
    private void CopyChannel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string name }
            || !Enum.TryParse<OutputChannel>(name, out var channel))
            return;

        try
        {
            Clipboard.SetText(_server.BroadcastAddressFor(channel));
            ChannelCopied.Text = "✓ Copiada. Pegala en un «Browser Source» nuevo de OBS.";
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo copiar la dirección del canal {Channel}", name);
            ChannelCopied.Text = "No se pudo copiar ahora: probá de nuevo.";
        }
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

    private void EditStyles_Click(object sender, RoutedEventArgs e) => _editStyles(this);

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
