using EcclesiaCast.Presentation.Remote;

namespace EcclesiaCast.Presentation.Services;

/// <summary>The windows of «Transmisión»: the pages for OBS and how they look.</summary>
public interface IBroadcastDialog
{
    /// <summary>
    /// «Transmisión → Salida para OBS…»: the addresses of the pages. Returns when it closes.
    /// <paramref name="obsPinChanged"/> gets the new PIN when the operator changes OBS's.
    /// </summary>
    void ShowPages(RemoteControlServer server, Action<string> obsPinChanged);

    /// <summary>«Transmisión → Estilos de la transmisión…». Returns when it closes.</summary>
    void ShowStyles();
}
