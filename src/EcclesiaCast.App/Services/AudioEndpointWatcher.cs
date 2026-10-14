using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
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
/// </summary>
public static class AudioEndpointWatcher
{
    private static int _generation;
    private static IMMDeviceEnumerator? _enumerator;
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
            _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            _client = new Client();
            var hr = _enumerator.RegisterEndpointNotificationCallback(_client);
            if (hr != 0)
                throw new COMException("RegisterEndpointNotificationCallback", hr);
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

    // ── COM ──────────────────────────────────────────────────────

    private sealed class Client : IMMNotificationClient
    {
        // Called by Windows on its own threads: only count, log and hand over.
        public int OnDeviceStateChanged(string deviceId, int newState)
        {
            Interlocked.Increment(ref _generation);
            Log.Information("Cambió el estado de un dispositivo de audio ({State})", newState);
            return 0;
        }

        public int OnDeviceAdded(string deviceId) => 0;

        public int OnDeviceRemoved(string deviceId)
        {
            Interlocked.Increment(ref _generation);
            Log.Information("Se quitó un dispositivo de audio");
            return 0;
        }

        public int OnDefaultDeviceChanged(int flow, int role, string defaultDeviceId)
        {
            // Flow 0 is playback; a new microphone does not move the sound.
            if (flow != 0)
                return 0;

            Interlocked.Increment(ref _generation);
            Log.Information("Cambió la salida de audio predeterminada de Windows");
            NotifyOnUiThread();
            return 0;
        }

        public int OnPropertyValueChanged(string deviceId, PropertyKey key) => 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public int PropertyId;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject
    {
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IntPtr endpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IMMNotificationClient client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }

    [ComImport, Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMNotificationClient
    {
        [PreserveSig]
        int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int newState);

        [PreserveSig]
        int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        [PreserveSig]
        int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        [PreserveSig]
        int OnDefaultDeviceChanged(int flow, int role, [MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);

        [PreserveSig]
        int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, PropertyKey key);
    }
}
