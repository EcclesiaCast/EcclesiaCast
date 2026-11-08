using EcclesiaCast.Core.Audio;
using LibVLCSharp.Shared;
using Serilog;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The volume of ONE LibVLC player without touching the others. LibVLC's own
/// Volume and Mute go to the Windows audio session that every player of the
/// program shares (see <see cref="SoundLevel"/>): they are kept at full and
/// unmuted, and the player is turned down with its own equalizer instead. A
/// silenced video does not get a muted output: it is opened without its sound
/// (or its sound track is switched off while it plays).
/// </summary>
public static class PlayerSound
{
    /// <summary>
    /// Leaves the shared session at full volume and unmuted. Called on every
    /// player before it plays, which also lifts a mute that an older version of
    /// the program (or Windows' volume mixer) left saved on that session.
    /// </summary>
    public static void KeepSessionFull(MediaPlayer player)
    {
        try
        {
            if (player.Mute)
                player.Mute = false;
            if (player.Volume != 100)
                player.Volume = 100;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo dejar el sonido compartido al máximo");
        }
    }

    /// <summary>Turns this player alone down to <paramref name="volume"/> (0–100).</summary>
    public static void SetVolume(MediaPlayer player, int volume)
    {
        try
        {
            if (SoundLevel.Split(volume) is not { } level)
            {
                player.UnsetEqualizer();
                return;
            }

            using var equalizer = new Equalizer();
            equalizer.SetPreamp((float)level.Preamp);
            for (uint band = 0; band < equalizer.BandCount; band++)
                equalizer.SetAmp((float)level.Bands, band);
            // LibVLC copies the settings: the equalizer can go right away.
            player.SetEqualizer(equalizer);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo cambiar el volumen del reproductor");
        }
    }
}
