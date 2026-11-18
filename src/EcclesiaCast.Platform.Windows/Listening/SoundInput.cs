using NAudio.CoreAudioApi;
using NAudio.Wave;
using EcclesiaCast.Presentation.Platform;
using Serilog;

namespace EcclesiaCast.Platform.Windows.Listening;

/// <summary>
/// Listens to a sound input and keeps the last half minute as the speech
/// recogniser wants it: one channel, 16.000 samples a second. Windows hands
/// the sound over on a thread of its own; everything here is safe to call
/// from any thread.
/// </summary>
public sealed class SoundInput : IDisposable
{
    public const int Rate = 16000;
    private const int Capacity = Rate * 30;

    private readonly object _gate = new();
    private readonly float[] _ring = new float[Capacity];
    private long _written;
    private float _peak;
    private WasapiCapture? _capture;
    private MMDevice? _device;

    // Turning the device's rate into 16 kHz: every output sample is the mean
    // of the input samples that fall in it (enough of a filter for speech).
    private double _step;
    private double _phase;
    private double _sum;
    private int _count;

    /// <summary>Samples heard since <see cref="Start"/>: the clock of what was listened to.</summary>
    public long SamplesHeard
    {
        get { lock (_gate) return _written; }
    }

    /// <summary>The loudest sample since the last time this was read, 0 to 1: for the level meter.</summary>
    public float TakePeak()
    {
        lock (_gate)
        {
            var peak = _peak;
            _peak = 0;
            return peak;
        }
    }

    /// <summary>The inputs Windows has now, the default one first, and the outputs to listen to for a trial.</summary>
    public static IReadOnlyList<SoundInputDevice> Devices()
    {
        var list = new List<SoundInputDevice> { new(string.Empty, "La entrada predeterminada de Windows", false) };
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                list.Add(new SoundInputDevice(device.ID, device.FriendlyName, false));
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                list.Add(new SoundInputDevice(device.ID, $"Lo que suena en esta PC: {device.FriendlyName} (para probar)", true));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudieron listar las entradas de sonido");
        }

        return list;
    }

    public void Start(SoundInputDevice input)
    {
        Stop();

        var enumerator = new MMDeviceEnumerator();
        _device = input.Id.Length == 0
            ? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console)
            : enumerator.GetDevice(input.Id);

        _capture = input.Loopback ? new WasapiLoopbackCapture(_device) : new WasapiCapture(_device);
        var format = _capture.WaveFormat;
        lock (_gate)
        {
            _written = 0;
            _peak = 0;
            _step = (double)format.SampleRate / Rate;
            _phase = 0;
            _sum = 0;
            _count = 0;
        }

        _capture.DataAvailable += (_, e) => Take(e.Buffer, e.BytesRecorded, format);
        _capture.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null)
                Log.Warning(e.Exception, "La entrada de sonido se cortó");
        };
        _capture.StartRecording();
        Log.Information("Escuchando la entrada de sonido «{Name}» ({Rate} Hz, {Channels} canales)",
            _device.FriendlyName, format.SampleRate, format.Channels);
    }

    public void Stop()
    {
        var capture = _capture;
        _capture = null;
        if (capture is not null)
        {
            try { capture.StopRecording(); }
            catch (Exception ex) { Log.Debug(ex, "Al parar la entrada de sonido"); }
            capture.Dispose();
        }

        _device?.Dispose();
        _device = null;
    }

    /// <summary>
    /// The sound between two moments of <see cref="SamplesHeard"/>. What is
    /// older than the half minute kept is left out.
    /// </summary>
    public float[] Between(long from, long to)
    {
        lock (_gate)
        {
            to = Math.Min(to, _written);
            from = Math.Max(from, Math.Max(0, _written - Capacity));
            if (to <= from)
                return [];

            var samples = new float[to - from];
            for (var i = 0; i < samples.Length; i++)
                samples[i] = _ring[(from + i) % Capacity];
            return samples;
        }
    }

    private void Take(byte[] buffer, int bytes, WaveFormat format)
    {
        var channels = format.Channels;
        var isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat
                      || (format is WaveFormatExtensible ext && ext.SubFormat == NAudio.Dmo.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT);
        var bytesPerSample = format.BitsPerSample / 8;
        var frame = bytesPerSample * channels;
        if (frame == 0)
            return;

        lock (_gate)
        {
            for (var offset = 0; offset + frame <= bytes; offset += frame)
            {
                float mono = 0;
                for (var c = 0; c < channels; c++)
                {
                    var at = offset + c * bytesPerSample;
                    mono += (isFloat, bytesPerSample) switch
                    {
                        (true, 4) => BitConverter.ToSingle(buffer, at),
                        (false, 2) => BitConverter.ToInt16(buffer, at) / 32768f,
                        (false, 3) => ((buffer[at + 2] << 24 | buffer[at + 1] << 16 | buffer[at] << 8) >> 8) / 8388608f,
                        (false, 4) => BitConverter.ToInt32(buffer, at) / 2147483648f,
                        _ => 0,
                    };
                }

                mono /= channels;
                _sum += mono;
                _count++;
                _phase += 1;
                if (_phase < _step)
                    continue;

                _phase -= _step;
                var sample = (float)(_sum / _count);
                _sum = 0;
                _count = 0;
                _ring[_written % Capacity] = sample;
                _written++;
                var size = Math.Abs(sample);
                if (size > _peak)
                    _peak = size;
            }
        }
    }

    public void Dispose() => Stop();
}
