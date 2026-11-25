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
            // The shared session carries the general volume of the program
            // (the «Salida» control): Windows moves it smoothly, with no
            // restart of the sound, unlike an equalizer.
            var master = _sessionNow;
            if (player.Volume != master)
                player.Volume = master;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo dejar el sonido compartido al máximo");
        }
    }

    private static int _master = 100;

    /// <summary>
    /// One volume for everything the program plays (0–100), set from the LIVE
    /// controls. It is the volume of the program's Windows audio session, so
    /// every player follows it at once and smoothly.
    /// </summary>
    public static int Master
    {
        get => _master;
        set
        {
            value = Math.Clamp(value, 0, 100);
            if (value == _master)
                return;
            _master = value;
            MasterChanged?.Invoke(null, EventArgs.Empty);
            GlideSession();
        }
    }

    /// <summary>The session volume the players have now, on its way to <see cref="_sessionTarget"/>.</summary>
    private static int _sessionNow = 100;

    private static int _sessionTarget = 100;

    private static System.Windows.Threading.DispatcherTimer? _glide;

    /// <summary>
    /// Raised while the session volume of the LibVLC players moves toward the
    /// bar: the bar is followed in short steps (about a tenth of a second) so
    /// a quick drag is a smooth change and never a staircase of jumps.
    /// </summary>
    public static event EventHandler? SessionChanged;

    private static void GlideSession()
    {
        _sessionTarget = SoundLevel.SessionVolume(_master);
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            // No window (tests): straight to the target.
            _sessionNow = _sessionTarget;
            SessionChanged?.Invoke(null, EventArgs.Empty);
            return;
        }

        if (!dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(GlideSession);
            return;
        }

        _glide ??= NewGlideTimer(dispatcher);
        if (!_glide.IsEnabled)
            _glide.Start();
    }

    private static System.Windows.Threading.DispatcherTimer NewGlideTimer(System.Windows.Threading.Dispatcher dispatcher)
    {
        var timer = new System.Windows.Threading.DispatcherTimer(
            System.Windows.Threading.DispatcherPriority.Send, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        timer.Tick += (_, _) =>
        {
            var gap = _sessionTarget - _sessionNow;
            if (gap == 0)
            {
                timer.Stop();
                return;
            }

            // A quarter of what is left each step (at least 1): about 100 ms to arrive.
            var step = Math.Max(1, Math.Abs(gap) / 4);
            _sessionNow += Math.Sign(gap) * Math.Min(step, Math.Abs(gap));
            SessionChanged?.Invoke(null, EventArgs.Empty);
        };
        return timer;
    }

    /// <summary>The first volume of a program that starts with the bar somewhere other than 100.</summary>
    public static void SnapSession()
    {
        _sessionTarget = SoundLevel.SessionVolume(_master);
        _sessionNow = _sessionTarget;
        SessionChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Raised when <see cref="Master"/> changes: each live player applies it.</summary>
    public static event EventHandler? MasterChanged;

    /// <summary>The player's own volume (0–100).</summary>
    public static int Effective(int volume) => Math.Clamp(volume, 0, 100);

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
