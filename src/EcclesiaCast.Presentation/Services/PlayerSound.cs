using EcclesiaCast.Core.Audio;
using LibVLCSharp.Shared;
using Serilog;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The volume of ONE LibVLC player without touching the others. LibVLC's own
/// Volume and Mute go to the Windows audio session that every player of the
/// program shares (see <see cref="SoundLevel"/>): they are kept at full and
/// unmuted, and the player is turned down with its own equalizer instead. A
/// silenced video does not get a muted output: it is opened without its sound
/// (or its sound track is switched off while it plays).
///
/// The exception is the music list's player (<see cref="VlcAudioPlayer"/>):
/// it plays through DirectSound and has a volume of its own. Windows still
/// turns it down with the shared session, so the general volume is also set
/// there directly (<see cref="ApplySession"/>).
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

    /// <summary>The session volume the players have now.</summary>
    private static volatile int _sessionNow = 100;

    private static volatile int _sessionTarget = 100;

    private static long _requestedAt;

    private static readonly AutoResetEvent Wake = new(false);

    private static Thread? _worker;

    private static readonly object WorkerLock = new();

    /// <summary>
    /// Raised when the session volume of the LibVLC players changes (on a
    /// worker thread, never the interface's): each player follows the bar at
    /// once, with no ramp. LibVLC can hold a player's lock while it decodes a
    /// video, so asking it from the interface thread could freeze the slider.
    /// </summary>
    public static event EventHandler? SessionChanged;

    /// <summary>The slowest time, in ms, from the bar moving to the players having the volume (for the log).</summary>
    public static double WorstLatencyMs { get; private set; }

    private static void GlideSession()
    {
        _sessionTarget = SoundLevel.SessionVolume(_master);
        if (PlatformServices.Dispatcher is null)
        {
            // No window (tests): straight to the target.
            _sessionNow = _sessionTarget;
            SessionChanged?.Invoke(null, EventArgs.Empty);
            return;
        }

        Interlocked.CompareExchange(ref _requestedAt, System.Diagnostics.Stopwatch.GetTimestamp(), 0);
        EnsureWorker();
        Wake.Set();
    }

    private static void EnsureWorker()
    {
        lock (WorkerLock)
        {
            if (_worker is not null)
                return;

            _worker = new Thread(ApplyLoop) { IsBackground = true, Name = "Volumen de EcclesiaCast" };
            _worker.Start();
        }
    }

    private static void ApplyLoop()
    {
        while (true)
        {
            Wake.WaitOne();
            // Only the latest value counts: moves that came while the players
            // were busy are skipped, never queued.
            var target = _sessionTarget;
            if (target != _sessionNow)
            {
                _sessionNow = target;
                try { SessionChanged?.Invoke(null, EventArgs.Empty); }
                catch (Exception ex) { Log.Debug(ex, "No se pudo aplicar el volumen"); }
                ApplySession();
            }

            var started = Interlocked.Exchange(ref _requestedAt, 0);
            if (started != 0)
            {
                var ms = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if (ms > WorstLatencyMs)
                    WorstLatencyMs = ms;
                if (ms > 50)
                    Log.Warning("El volumen tardó {Ms:0} ms en llegar a los reproductores", ms);
            }
        }
    }

    /// <summary>The first volume of a program that starts with the bar somewhere other than 100.</summary>
    public static void SnapSession()
    {
        _sessionTarget = SoundLevel.SessionVolume(_master);
        _sessionNow = _sessionTarget;
        SessionChanged?.Invoke(null, EventArgs.Empty);
        ApplySession();
    }

    /// <summary>
    /// Gives the system's volume for the program (see
    /// <see cref="IProgramVolume"/>) the general volume. Also called before
    /// each song, since on Windows the session belongs to the output device of
    /// the moment and Windows remembers the one it had.
    /// </summary>
    public static void ApplySession()
    {
        try
        {
            PlatformServices.ProgramVolume?.Apply(SoundLevel.SessionScalar(_master));
        }
        catch (Exception ex)
        {
            // No sound output at all, for one.
            Log.Debug(ex, "No se pudo poner el volumen general en Windows");
        }
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
