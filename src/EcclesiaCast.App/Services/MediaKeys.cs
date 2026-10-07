using System.Runtime.InteropServices;

namespace EcclesiaCast.App.Services;

/// <summary>The multimedia keys of the keyboard, sent as if one had been pressed.</summary>
public enum MediaKey
{
    PreviousTrack = 0xB1,
    PlayPause = 0xB3,
    NextTrack = 0xB0,
    VolumeDown = 0xAE,
    VolumeUp = 0xAF,
    Mute = 0xAD,
}

/// <summary>
/// Controls whatever player is open (Spotify, the browser, Windows' own) the
/// way the keyboard's media keys do, without putting a player or a browser
/// inside the program. Windows hands the key to the player that last played.
/// </summary>
public static class MediaKeys
{
    private const uint KeyEventKeyUp = 0x0002;

    public static void Send(MediaKey key)
    {
        keybd_event((byte)key, 0, 0, UIntPtr.Zero);
        keybd_event((byte)key, 0, KeyEventKeyUp, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
