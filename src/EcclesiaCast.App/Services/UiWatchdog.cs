using System.Windows.Threading;
using EcclesiaCast.Core.Diagnostics;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Notices when the operator window stops answering and writes it in the log
/// with the latest actions, so a freeze that fixes itself after a few seconds
/// can be traced afterwards. A thread of its own posts a trivial message to the
/// window every half second; if one is not answered within two seconds the
/// window is frozen, and the answer that finally comes tells how long it lasted.
/// While everything works it costs one cheap wake-up per half second.
/// </summary>
public sealed class UiWatchdog(Dispatcher dispatcher)
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(2);

    /// <summary>
    /// A wake-up this late means the whole process was suspended (the computer
    /// slept), not that the window hung.
    /// </summary>
    private static readonly TimeSpan Oversleep = TimeSpan.FromSeconds(5);

    private readonly FreezeDetector _detector = new(Threshold);
    private readonly ManualResetEventSlim _stop = new();
    private Thread? _thread;

    public void Start()
    {
        _thread = new Thread(Run)
        {
            Name = "UiWatchdog",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        _thread.Start();
    }

    public void Stop() => _stop.Set();

    private void Run()
    {
        var lastWake = DateTimeOffset.Now;

        while (!_stop.Wait(Interval))
        {
            var now = DateTimeOffset.Now;
            if (now - lastWake > Interval + Oversleep)
            {
                _detector.Forgive();
                lastWake = now;
                continue;
            }

            lastWake = now;

            if (_detector.Check(now) is { Kind: FreezeEventKind.Frozen } frozen)
            {
                Log.Warning(
                    "Ventana congelada: hace {Seconds:0.0} s que no responde. Últimas acciones: {Trail}",
                    frozen.Duration.TotalSeconds, ActionTrail.Shared.Describe(now));
            }

            if (_detector.PingPending)
                continue;

            _detector.PingSent(now);
            try
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, Answered);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TaskCanceledException)
            {
                // The dispatcher is shutting down with the program.
                return;
            }
        }
    }

    /// <summary>Runs on the window's own thread: the proof that it is alive.</summary>
    private void Answered()
    {
        var now = DateTimeOffset.Now;
        if (_detector.PingAnswered(now) is { Kind: FreezeEventKind.Recovered } recovered)
        {
            Log.Warning(
                "Ventana descongelada: estuvo {Seconds:0.0} s sin responder. Últimas acciones: {Trail}",
                recovered.Duration.TotalSeconds, ActionTrail.Shared.Describe(now));
        }
    }
}
