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
    private readonly Action<string> _obsPinChanged;

    public BroadcastWindow(RemoteControlServer server, Action<Window> editStyles, Action<string> obsPinChanged)
    {
        InitializeComponent();
        _server = server;
        _editStyles = editStyles;
        _obsPinChanged = obsPinChanged;
        ShowAddresses();
    }

    private void ShowAddresses()
    {
        BroadcastBox.Text = _server.BroadcastAddress;
        StableNote.Text =
            "Esta dirección no cambia al reiniciar la PC: se pega una sola vez en OBS o vMix. " +
            $"Si OBS está en otra PC de la red, usá esta: {_server.NetworkBroadcastAddress}";
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

    private void ChangeObsPin_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = Notice.Confirm(
            "Se genera un PIN nuevo para OBS y vMix. Las direcciones que ya pegaste dejan de funcionar " +
            "hasta que copies las nuevas.\n\nEl PIN del celular no cambia.",
            yes: "Cambiar el PIN de OBS",
            title: "Cambiar el PIN de OBS");
        if (!confirmed)
            return;

        _obsPinChanged(_server.ChangeObsPin());
        ShowAddresses();
        ChannelCopied.Text = "✓ PIN de OBS cambiado. Copiá las direcciones nuevas.";
    }

    private void EditStyles_Click(object sender, RoutedEventArgs e) => _editStyles(this);

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
