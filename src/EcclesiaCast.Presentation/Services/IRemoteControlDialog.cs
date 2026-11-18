using EcclesiaCast.Presentation.Remote;

namespace EcclesiaCast.Presentation.Services;

/// <summary>The window with the address and the QR code of the phone control.</summary>
public interface IRemoteControlDialog
{
    /// <summary>
    /// Shows it and returns when it closes: true when the operator asked to
    /// turn the control off.
    /// </summary>
    bool Show(RemoteControlServer server);
}
