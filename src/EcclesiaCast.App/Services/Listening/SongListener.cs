using System.Diagnostics;
using System.Windows.Threading;
using EcclesiaCast.Core.Listening;
using Serilog;

namespace EcclesiaCast.App.Services.Listening;

/// <summary>What the listener made of the last seconds of sound.</summary>
/// <param name="Text">The words that were plausibly sung; empty when there were none.</param>
/// <param name="Seconds">How long the stretch listened to was.</param>
/// <param name="Cost">How long the recogniser took: above the pace, this PC is not keeping up.</param>
public readonly record struct HeardWindow(string Text, double Seconds, double Cost);

/// <summary>
/// Keeps listening while it is on: every few seconds it hands the last
/// stretch of sound to the recogniser and tells the window what was heard.
/// All the work happens on a thread of its own, at low priority; the window
/// only hears about results.
/// </summary>
public sealed class SongListener(Dispatcher dispatcher) : IDisposable
{
    /// <summary>How much sound is listened to each time.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(8);

    /// <summary>How often. The recogniser needs under three seconds for a window on a current PC.</summary>
    private static readonly TimeSpan Pace = TimeSpan.FromSeconds(3);

    /// <summary>Quieter than this there is nobody singing: the recogniser is not even asked.</summary>
    private const float Silence = 0.01f;

    private readonly SoundInput _input = new();
    private readonly SpeechEar _ear = new();
    private Thread? _thread;
    private CancellationTokenSource? _cancel;
    private long _since;

    /// <summary>Raised on the window's thread for every stretch listened to.</summary>
    public event Action<HeardWindow>? Heard;

    /// <summary>Raised on the window's thread when listening stops by itself, with why.</summary>
    public event Action<string>? Failed;

    public bool IsListening => _thread is not null;

    /// <summary>The level of the input since the last look, 0 to 1.</summary>
    public float TakeLevel() => _input.TakePeak();

    /// <summary>
    /// A slide just changed: the sound before this moment belongs to the
    /// slide that left and is not listened to again.
    /// </summary>
    public void MarkChange() => Interlocked.Exchange(ref _since, _input.SamplesHeard);

    public void Start(SoundInputDevice device)
    {
        Stop();
        _input.Start(device);
        _since = 0;
        _cancel = new CancellationTokenSource();
        var cancel = _cancel.Token;
        _thread = new Thread(() => Run(cancel))
        {
            Name = "SongListener",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cancel?.Cancel();
        _cancel = null;
        _thread = null;
        _input.Stop();
    }

    private void Run(CancellationToken cancel)
    {
        try
        {
            lock (_ear)
                _ear.Load();
            long last = 0;

            while (!cancel.IsCancellationRequested)
            {
                var now = _input.SamplesHeard;
                if (now - last < Pace.TotalSeconds * SoundInput.Rate)
                {
                    cancel.WaitHandle.WaitOne(150);
                    continue;
                }

                last = now;
                var from = Math.Max(Interlocked.Read(ref _since), now - (long)(Window.TotalSeconds * SoundInput.Rate));
                var samples = _input.Between(from, now);
                var seconds = samples.Length / (double)SoundInput.Rate;
                if (seconds < 1.5)
                    continue;

                var text = string.Empty;
                var clock = Stopwatch.StartNew();
                if (Loudest(samples) >= Silence)
                {
                    lock (_ear)
                        text = HeardText.Clean(_ear.Transcribe(samples, cancel), seconds);
                }

                var heard = new HeardWindow(text, seconds, clock.Elapsed.TotalSeconds);
                dispatcher.BeginInvoke(() =>
                {
                    if (!cancel.IsCancellationRequested)
                        Heard?.Invoke(heard);
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Turned off.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "El seguimiento por audio se detuvo");
            var reason = ex is DllNotFoundException or BadImageFormatException or TypeInitializationException
                ? "Esta PC no puede correr el reconocedor de voz."
                : $"Se detuvo: {ex.Message}";
            dispatcher.BeginInvoke(() => Failed?.Invoke(reason));
        }
    }

    private static float Loudest(float[] samples)
    {
        float peak = 0;
        foreach (var sample in samples)
            peak = Math.Max(peak, Math.Abs(sample));
        return peak;
    }

    public void Dispose()
    {
        Stop();
        _input.Dispose();
        _ear.Dispose();
    }
}
