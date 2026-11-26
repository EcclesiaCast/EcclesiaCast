using System.Diagnostics;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Controls;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// While the output is on and a background video plays, a line in the log
/// every ten seconds: frames drawn per second, frames let go, and the memory
/// of the program. It is what the test on the Mac measures (28 or more for a
/// video of 30, less than 1 % lost, memory not climbing more than 50 MB), as
/// the prototype of batch 1 did.
/// </summary>
public sealed class VideoReport
{
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(10);

    private readonly Func<VlcVideoSurface?> _video;
    private readonly DispatcherTimer _timer = new() { Interval = Every };
    private readonly Stopwatch _clock = new();
    private long _lastDrawn;
    private long _lastDropped;
    private long _startMemory;

    public VideoReport(Func<VlcVideoSurface?> video)
    {
        _video = video;
        _timer.Tick += (_, _) => Report();
    }

    public void Start()
    {
        if (_timer.IsEnabled)
            return;

        _startMemory = Process.GetCurrentProcess().WorkingSet64;
        _clock.Restart();
        Remember();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void Remember()
    {
        _lastDrawn = _video()?.FramesDrawn ?? 0;
        _lastDropped = _video()?.FramesDropped ?? 0;
    }

    private void Report()
    {
        var seconds = Math.Max(0.001, _clock.Elapsed.TotalSeconds);
        _clock.Restart();
        var video = _video();
        var drawn = (video?.FramesDrawn ?? 0) - _lastDrawn;
        var dropped = (video?.FramesDropped ?? 0) - _lastDropped;
        Remember();

        // Nothing moving (no video, or paused under the logo): nothing to say.
        if (drawn <= 0 && dropped <= 0)
            return;

        var memory = Process.GetCurrentProcess().WorkingSet64;
        var handed = drawn + dropped;
        Log.Information(
            "Video: {Drawn:0.0} cuadros pintados/s, {Dropped} descartados ({DroppedPct:0.0} %) " +
            "| total pintados {TotalDrawn}, descartados {TotalDropped} " +
            "| memoria {Memory} MB ({Delta:+0;-0} MB desde que se prendió la salida), .NET {Managed} MB",
            drawn / seconds, dropped, handed == 0 ? 0 : 100.0 * dropped / handed,
            video?.FramesDrawn ?? 0, video?.FramesDropped ?? 0,
            memory / 1048576, (memory - _startMemory) / 1048576, GC.GetTotalMemory(false) / 1048576);
    }
}
