using EcclesiaCast.Presentation.Platform;
using NAudio.CoreAudioApi;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// The program's Windows audio session. It is set here without a player: the
/// video's players only write it while they have a sound output open, and the
/// music list's one never does. The session belongs to the output device of the
/// moment, and Windows remembers the volume it had on each one.
/// </summary>
public sealed class WindowsProgramVolume : IProgramVolume
{
    public void Apply(float volume)
    {
        using var devices = new MMDeviceEnumerator();
        using var device = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var session = device.AudioSessionManager.SimpleAudioVolume;
        session.Volume = volume;
        session.Mute = false;
    }
}
