using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using EcclesiaCast.Presentation.Services;
using Serilog;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// Notices when Windows changes, adds or removes an audio output (the speakers
/// the operator picks in the taskbar, headphones, a Bluetooth speaker…) and
/// tells <see cref="AudioOutputChanges"/>, which the players watch.
///
/// It talks to Windows through NAudio, like the sound input of «Seguir la
/// canción». It used to declare the same Windows component on its own, and
/// .NET then handed NAudio this watcher's type for it: listening failed with
/// «Unable to cast MMDeviceEnumeratorComObject». One declaration only.
/// </summary>
public static class AudioEndpointWatcher
{
    private static MMDeviceEnumerator? _enumerator;
    private static Client? _client;

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

    // ── Avisos de Windows ────────────────────────────────────────

    private sealed class Client : IMMNotificationClient
    {
        // Called by Windows on its own threads: only count, log and hand over.
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            AudioOutputChanges.Count();
            var playback = IsPlayback(deviceId);
            Log.Information("Cambió el estado de un dispositivo de audio ({State}, {Kind})",
                newState, playback ? "salida" : "entrada");

            // An output that comes back (a cable with a loose contact, a
            // projector that renegotiates its HDMI) stays the default one, so
            // Windows reports no change of default: without this, what was
            // playing kept its dead output until the program was restarted.
            if (playback && newState == DeviceState.Active)
                AudioOutputChanges.NotifyOnUiThread();
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
            AudioOutputChanges.Count();
            Log.Information("Se quitó un dispositivo de audio");
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            // A new microphone does not move the sound.
            if (flow != DataFlow.Render)
                return;

            AudioOutputChanges.Count();
            Log.Information("Cambió la salida de audio predeterminada de Windows");
            AudioOutputChanges.NotifyOnUiThread();
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }
}
