using EcclesiaCast.Presentation.Remote;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// What the tutorials need from here to open the phone and OBS windows beside
/// their cards. The windows themselves are made by each interface.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>The local server, turned on if it was off; null when it could not start.</summary>
    public RemoteControlServer? RemoteForTutorial() => StartRemote() ? _remote : null;

    /// <summary>Keeps the phone's new PIN.</summary>
    public void KeepRemotePin(string pin) => _settings.Set(RemotePinKey, pin);

    /// <summary>Keeps OBS and vMix's new PIN.</summary>
    public void KeepObsPin(string pin) => _settings.Set(RemoteObsPinKey, pin);
}
