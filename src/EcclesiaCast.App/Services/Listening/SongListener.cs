using System.Diagnostics;
using System.Windows.Threading;
using EcclesiaCast.Core.Listening;
using Serilog;

namespace EcclesiaCast.App.Services.Listening;

/// <summary>What the listener made of the last seconds of sound.</summary>
/// <param name="Text">The words that were plausibly sung; empty when there were none.</param>
/// <param name="Seconds">How long the stretch listened to was.</param>
/// <param name="Cost">How long the recogniser took: the singing went on meanwhile.</param>
/// <param name="Level">The loudest moment of the stretch, 0 to 1, before it was evened out.</param>
public readonly record struct HeardWindow(string Text, double Seconds, double Cost, float Level);

/// <summary>
/// Keeps listening while it is on: as soon as the recogniser is free it hands
/// it the last few seconds of sound and tells the window what was heard. All
/// the work happens on a thread of its own, at low priority; the window only
/// hears about results.
/// </summary>
public sealed class SongListener(Dispatcher dispatcher) : IDisposable
{
    /// <summary>
    /// How much sound is listened to each time: a sung line and a bit. Longer
    /// is slower and drags in the line before; shorter cuts words in half.
    /// </summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The least time between two looks. With the video card a look takes
    /// half a second, so this is what sets the pace; on the processor it is
    /// the recogniser itself.
    /// </summary>
    private static readonly TimeSpan Pace = TimeSpan.FromSeconds(1);

    /// <summary>Quieter than this nothing is coming in at all (a muted input): the recogniser is not even asked.</summary>
    public const float Silence = 0.002f;

    /// <summary>
    /// A quiet input (a notebook microphone across the room) is raised to
    /// this loudness before the recogniser hears it, up to <see cref="MaxGain"/> times.
    /// </summary>
    private const float Target = 0.5f;

    private const float MaxGain = 40f;

    private readonly SoundInput _input = new();
    private readonly SpeechEar _ear = new(Window);
    private Thread? _thread;
    private CancellationTokenSource? _cancel;

    /// <summary>Raised on the window's thread for every stretch listened to.</summary>
    public event Action<HeardWindow>? Heard;

    /// <summary>Raised on the window's thread when listening stops by itself, with why.</summary>
    public event Action<string>? Failed;

    /// <summary>Raised on the window's thread once the recogniser is ready, with true when it runs on the video card.</summary>
    public event Action<bool>? Ready;

    public bool IsListening => _thread is not null;

    /// <summary>The level of the input since the last look, 0 to 1.</summary>
    public float TakeLevel() => _input.TakePeak();

    public void Start(SoundInputDevice device)
    {
        Stop();
        _input.Start(device);
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
            var onCard = _ear.OnVideoCard;
            dispatcher.BeginInvoke(() =>
            {
                if (!cancel.IsCancellationRequested)
                    Ready?.Invoke(onCard);
            });

            long last = 0;
            var pace = (long)(Pace.TotalSeconds * SoundInput.Rate);
            var window = (long)(Window.TotalSeconds * SoundInput.Rate);

            while (!cancel.IsCancellationRequested)
            {
                var now = _input.SamplesHeard;
                if (now - last < pace)
                {
                    cancel.WaitHandle.WaitOne(50);
                    continue;
                }

                last = now;
                var samples = _input.Between(now - window, now);
                var seconds = samples.Length / (double)SoundInput.Rate;
                if (seconds < 1.5)
                    continue;

                var text = string.Empty;
                var clock = Stopwatch.StartNew();
                var level = Loudest(samples);
                if (level >= Silence)
                {
                    EvenOut(samples, level);
                    lock (_ear)
                        text = HeardText.Clean(_ear.Transcribe(samples, cancel), seconds);
                }

                var heard = new HeardWindow(text, seconds, clock.Elapsed.TotalSeconds, level);
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

    /// <summary>Raises a quiet stretch to a steady loudness; a loud one is left alone.</summary>
    private static void EvenOut(float[] samples, float peak)
    {
        var gain = Math.Min(Target / peak, MaxGain);
        if (gain <= 1)
            return;
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= gain;
    }

    public void Dispose()
    {
        Stop();
        _input.Dispose();
        _ear.Dispose();
    }
}
