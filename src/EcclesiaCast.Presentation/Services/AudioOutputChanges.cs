using EcclesiaCast.Presentation.Platform;
using Serilog;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// What the players need to know when the system changes, adds or removes an
/// audio output (the speakers the operator picks in the taskbar, headphones,
/// a Bluetooth speaker…). The system's own watcher (on Windows,
/// <c>AudioEndpointWatcher</c>) reports here.
///
/// LibVLC 3 keeps the audio output it opened inside each player. When the
/// system swaps the device underneath, that output can stay dead until the
/// player is rebuilt: the church heard nothing until the program was closed
/// and opened again. The players compare <see cref="Generation"/> before each
/// play and open a fresh output when it moved, and listen to
/// <see cref="OutputChanged"/> to reopen the one that is playing right now.
/// </summary>
public static class AudioOutputChanges
{
    private static int _generation;
    private static IUiTimer? _debounce;

    /// <summary>Moves every time the system changes an audio device; read it before playing.</summary>
    public static int Generation => Volatile.Read(ref _generation);

    /// <summary>
    /// Raised on the UI thread, a moment after the system moved the sound to
    /// another device (or the operator asked to restart the sound): whatever is
    /// playing should reopen its audio output.
    /// </summary>
    public static event EventHandler? OutputChanged;

    /// <summary>A device changed. Safe from any thread.</summary>
    public static void Count() => Interlocked.Increment(ref _generation);

    /// <summary>
    /// «Reiniciar el sonido»: the same as if the system had changed the device.
    /// For when the sound is dead and nothing was noticed.
    /// </summary>
    public static void RequestRestart()
    {
        Interlocked.Increment(ref _generation);
        Log.Information("Se pidió reiniciar el sonido");
        NotifyOnUiThread();
    }

    /// <summary>Raises <see cref="OutputChanged"/> on the UI thread, once the reports have settled. Safe from any thread.</summary>
    public static void NotifyOnUiThread()
    {
        if (PlatformServices.Dispatcher is not { } dispatcher || PlatformServices.Timers is not { } timers)
            return;

        dispatcher.Post(() =>
        {
            // The system reports one change several times (once per role): wait
            // until it settles, and reopen the sound once.
            _debounce ??= CreateDebounce(timers);
            _debounce.Stop();
            _debounce.Start();
        });
    }

    private static IUiTimer CreateDebounce(IUiTimerFactory timers)
    {
        var timer = timers.Create(TimeSpan.FromMilliseconds(700));
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                OutputChanged?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falló al reabrir la salida de audio");
            }
        };
        return timer;
    }
}
