using System.Windows;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Notices when Windows changes, adds or removes an audio output (the speakers
/// the operator picks in the taskbar, headphones, a Bluetooth speaker…).
///
/// LibVLC 3 keeps the audio output it opened inside each player. When Windows
/// swaps the device underneath, that output can stay dead until the player is
/// rebuilt: the church heard nothing until the program was closed and opened
/// again. The players compare <see cref="Generation"/> before each play and
/// open a fresh output when it moved, and listen to <see cref="OutputChanged"/>
/// to reopen the one that is playing right now.
///
/// It talks to Windows through NAudio, like the sound input of «Seguir la
/// canción». It used to declare the same Windows component on its own, and
/// .NET then handed NAudio this watcher's type for it: listening failed with
/// «Unable to cast MMDeviceEnumeratorComObject». One declaration only.
/// </summary>
public static class AudioEndpointWatcher
{
    private static int _generation;
    private static MMDeviceEnumerator? _enumerator;
    private static Client? _client;
    private static DispatcherTimer? _debounce;

    /// <summary>Moves every time Windows changes an audio device; read it before playing.</summary>
    public static int Generation => Volatile.Read(ref _generation);

    /// <summary>
    /// Raised on the UI thread, a moment after Windows moved the sound to
    /// another device (or the operator asked to restart the sound): whatever is
    /// playing should reopen its audio output.
    /// </summary>
    public static event EventHandler? OutputChanged;

    public static void Start()
    {
        if (_enumerator is not null)
            return;

        try
        {
            _enumerator = new MMDeviceEnumerator();
            _client = new Client();
            var hr = _enumerator.RegisterEndpointNotificationCallback(_client);
            if (hr != 0)
                throw new System.Runtime.InteropServices.COMException("RegisterEndpointNotificationCallback", hr);
        }
        catch (Exception ex)
        {
            // Without the watcher the sound behaves as it always did.
            Log.Warning(ex, "No se pudo vigilar los dispositivos de audio de Windows");
            _enumerator = null;
            _client = null;
        }
    }

    public static void Stop()
    {
        try
        {
            if (_enumerator is not null && _client is not null)
                _ = _enumerator.UnregisterEndpointNotificationCallback(_client);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudo dejar de vigilar los dispositivos de audio");
        }

        _client = null;
        _enumerator?.Dispose();
        _enumerator = null;
    }

    /// <summary>
    /// «Reiniciar el sonido»: the same as if Windows had changed the device.
    /// For when the sound is dead and nothing was noticed.
    /// </summary>
    public static void RequestRestart()
    {
        Interlocked.Increment(ref _generation);
        Log.Information("Se pidió reiniciar el sonido");
        NotifyOnUiThread();
    }

    private static void NotifyOnUiThread()
    {
        if (Application.Current?.Dispatcher is not { } dispatcher)
            return;

        dispatcher.BeginInvoke(() =>
        {
            // Windows reports one change several times (once per role): wait
            // until it settles, and reopen the sound once.
            _debounce ??= CreateDebounce();
            _debounce.Stop();
            _debounce.Start();
        });
    }

    private static DispatcherTimer CreateDebounce()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
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

    // ── Avisos de Windows ────────────────────────────────────────

    private sealed class Client : IMMNotificationClient
    {
        // Called by Windows on its own threads: only count, log and hand over.
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Interlocked.Increment(ref _generation);
            var playback = IsPlayback(deviceId);
            Log.Information("Cambió el estado de un dispositivo de audio ({State}, {Kind})",
                newState, playback ? "salida" : "entrada");

            // An output that comes back (a cable with a loose contact, a
            // projector that renegotiates its HDMI) stays the default one, so
            // Windows reports no change of default: without this, what was
            // playing kept its dead output until the program was restarted.
            if (playback && newState == DeviceState.Active)
                NotifyOnUiThread();
        }

        /// <summary>
        /// Windows writes the direction in the id of the endpoint:
        /// «{0.0.0.…» is an output, «{0.0.1.…» an input (a microphone).
        /// </summary>
        private static bool IsPlayback(string? deviceId) =>
            deviceId is null || !deviceId.StartsWith("{0.0.1.", StringComparison.Ordinal);

        public void OnDeviceAdded(string pwstrDeviceId) { }

        public void OnDeviceRemoved(string deviceId)
        {
            Interlocked.Increment(ref _generation);
            Log.Information("Se quitó un dispositivo de audio");
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            // A new microphone does not move the sound.
            if (flow != DataFlow.Render)
                return;

            Interlocked.Increment(ref _generation);
            Log.Information("Cambió la salida de audio predeterminada de Windows");
            NotifyOnUiThread();
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }
}
