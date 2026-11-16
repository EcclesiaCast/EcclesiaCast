using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using Serilog;

namespace EcclesiaCast.Proto;

/// <summary>
/// A looping background video, the same way as VlcVideoSurface in the
/// program: LibVLC hands every frame over in memory and it is copied into a
/// WriteableBitmap on the UI thread, so the lyrics are an ordinary layer on
/// top. The same rules, learned the hard way there:
/// • ONE player for the whole life of this object, with the callback
///   delegates kept in fields. A collected delegate that the native side
///   still calls kills the process with no managed exception.
/// • The format callback never waits for the UI thread.
/// Any number of <see cref="Image"/> controls can show the picture.
/// </summary>
public sealed class VideoBackground : IDisposable
{
    private const int Alignment = 64;
    private static readonly TimeSpan ReportEvery = TimeSpan.FromSeconds(10);

    private readonly LibVLC _engine;
    private readonly MediaPlayer _player;
    private readonly object _sync = new();

    // Assigned once and kept alive for as long as the player exists.
    private readonly MediaPlayer.LibVLCVideoFormatCb _formatCb;
    private readonly MediaPlayer.LibVLCVideoCleanupCb _cleanupCb;
    private readonly MediaPlayer.LibVLCVideoLockCb _lockCb;
    private readonly MediaPlayer.LibVLCVideoDisplayCb _displayCb;

    private IntPtr _rawBuffer, _buffer;
    private int _bufferSize;
    private int _width, _height, _stride;
    private int _framePending;
    private int _generation;

    private WriteableBitmap? _bitmap;
    private readonly List<(Image Image, TimeSpan Every)> _viewers = [];
    private readonly Dictionary<Image, long> _lastViewerPaint = [];

    // Counters for the 10-second report.
    private long _delivered, _painted, _dropped, _darkDrops, _formats;
    private double _lastBrightness = -1;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _reportTimer;
    private readonly DispatcherTimer _loopTimer;
    private long _lastTime = -1;
    private int _loops;
    private long _startMemory;
    private TimeSpan _lastReport;
    private long _lastDelivered, _lastPainted, _lastDropped;

    public string? CurrentFile { get; private set; }

    public bool IsPlaying => CurrentFile is not null;

    /// <summary>Raised on the UI thread when the picture object changes (a new size).</summary>
    public event EventHandler<WriteableBitmap?>? BitmapChanged;

    public VideoBackground(LibVLC engine)
    {
        _engine = engine;
        _player = new MediaPlayer(engine)
        {
            // The graphics card decodes and VLC copies each frame back for the
            // callbacks (VideoToolbox on a Mac).
            EnableHardwareDecoding = true,
        };

        _formatCb = OnFormat;
        _cleanupCb = OnCleanup;
        _lockCb = OnLock;
        _displayCb = OnDisplay;
        _player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
        _player.SetVideoCallbacks(_lockCb, null, _displayCb);

        _player.EncounteredError += (_, _) => Log.Error("VLC no pudo reproducir el video de fondo");
        _player.Playing += (_, _) => Log.Information("Video: reproduciendo");

        _reportTimer = new DispatcherTimer { Interval = ReportEvery };
        _reportTimer.Tick += (_, _) => Report();
        _loopTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _loopTimer.Tick += (_, _) => WatchLoop();
    }

    /// <summary>
    /// Shows the picture in <paramref name="image"/>, repainting it at most
    /// every <paramref name="every"/> (zero: every frame).
    /// </summary>
    public void Attach(Image image, TimeSpan every)
    {
        _viewers.Add((image, every));
        image.Source = _bitmap;
    }

    public void Detach(Image image)
    {
        _viewers.RemoveAll(v => v.Image == image);
        _lastViewerPaint.Remove(image);
        image.Source = null;
    }

    /// <summary>
    /// Plays a file in a loop. <paramref name="withSound"/> lets the HDMI sound
    /// check hear it; a background video is silent otherwise.
    /// </summary>
    public void Play(string path, bool withSound)
    {
        Interlocked.Increment(ref _generation);
        using var media = new Media(_engine, new Uri(path));
        // Loops with no gap: VLC opens the file again by itself, and the last
        // frame stays in the bitmap until the first new one is painted.
        media.AddOption(":input-repeat=65535");
        media.AddOption(":file-caching=500");
        if (!withSound)
            media.AddOption(":no-audio");

        _player.Play(media);
        CurrentFile = path;

        _delivered = _painted = _dropped = _darkDrops = _formats = 0;
        _lastDelivered = _lastPainted = _lastDropped = 0;
        _loops = 0;
        _lastTime = -1;
        _lastBrightness = -1;
        _lastReport = _clock.Elapsed;
        _startMemory = Process.GetCurrentProcess().WorkingSet64;
        _reportTimer.Start();
        _loopTimer.Start();

        Log.Information("Video de fondo: {File}, sonido {Sound} (memoria al empezar {Memory} MB)",
            path, withSound ? "sí" : "no", _startMemory / 1048576);
    }

    public void Stop()
    {
        Interlocked.Increment(ref _generation);
        _reportTimer.Stop();
        _loopTimer.Stop();
        if (CurrentFile is not null)
            Report();
        CurrentFile = null;
        try { _player.Stop(); } catch (Exception ex) { Log.Warning(ex, "Fallo al parar el video"); }

        _bitmap = null;
        foreach (var viewer in _viewers)
            viewer.Image.Source = null;
        BitmapChanged?.Invoke(this, null);
        Log.Information("Video de fondo: apagado");
    }

    public void Dispose()
    {
        _reportTimer.Stop();
        _loopTimer.Stop();
        try { _player.Stop(); } catch { /* closing anyway */ }
        try { _player.Dispose(); } catch { /* closing anyway */ }

        lock (_sync)
        {
            if (_rawBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_rawBuffer);
                _rawBuffer = _buffer = IntPtr.Zero;
                _bufferSize = 0;
            }
        }
    }

    // ── Callbacks de VLC (hilo de decodificación) ────────────────

    private uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        try
        {
            var w = (int)width;
            var h = (int)height;
            var stride = (w * 4 + Alignment - 1) & ~(Alignment - 1);
            var needed = stride * h;

            // RV32: B, G, R, X in memory — Avalonia's Bgra8888.
            Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);
            pitches = (uint)stride;
            lines = (uint)h;

            lock (_sync)
            {
                // The buffer only grows, so a frame in flight never points at
                // memory just freed.
                if (needed > _bufferSize)
                {
                    if (_rawBuffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(_rawBuffer);
                    _rawBuffer = Marshal.AllocHGlobal(needed + Alignment);
                    _buffer = (IntPtr)(((long)_rawBuffer + Alignment - 1) & ~((long)Alignment - 1));
                    _bufferSize = needed;
                }

                _width = w;
                _height = h;
                _stride = stride;
            }

            Interlocked.Increment(ref _formats);
            Log.Debug("Video: formato {Width}×{Height}", w, h);
            return 1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fallo al negociar el formato de video");
            return 0;
        }
    }

    private void OnCleanup(ref IntPtr opaque)
    {
        // The buffer is reused across formats and freed in Dispose.
    }

    private IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        lock (_sync)
        {
            Marshal.WriteIntPtr(planes, _buffer);
            return _buffer;
        }
    }

    private void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        Interlocked.Increment(ref _delivered);

        // Backpressure: drop a frame instead of queueing it while the UI is
        // busy — a growing queue is what makes playback stutter.
        if (Interlocked.CompareExchange(ref _framePending, 1, 0) != 0)
        {
            Interlocked.Increment(ref _dropped);
            return;
        }

        var generation = Volatile.Read(ref _generation);
        Dispatcher.UIThread.Post(() => Paint(generation), DispatcherPriority.Render);
    }

    // ── Hilo de la pantalla ──────────────────────────────────────

    private void Paint(int generation)
    {
        try
        {
            lock (_sync)
            {
                if (generation != _generation || _buffer == IntPtr.Zero || _width <= 0 || _height <= 0)
                    return;

                if (_bitmap is null || _bitmap.PixelSize.Width != _width || _bitmap.PixelSize.Height != _height)
                {
                    _bitmap = new WriteableBitmap(
                        new PixelSize(_width, _height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
                    foreach (var viewer in _viewers)
                        viewer.Image.Source = _bitmap;
                    BitmapChanged?.Invoke(this, _bitmap);
                }

                CopyFrame(_bitmap);
            }

            _painted++;
            var now = _clock.ElapsedTicks;
            foreach (var (image, every) in _viewers)
            {
                if (every > TimeSpan.Zero
                    && _lastViewerPaint.TryGetValue(image, out var last)
                    && Stopwatch.GetElapsedTime(last, now) < every)
                    continue;
                _lastViewerPaint[image] = now;
                image.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Cuadro salteado");
        }
        finally
        {
            Interlocked.Exchange(ref _framePending, 0);
        }
    }

    private unsafe void CopyFrame(WriteableBitmap bitmap)
    {
        using var target = bitmap.Lock();
        var rowBytes = _width * 4;
        var source = (byte*)_buffer;
        var destination = (byte*)target.Address;
        for (var y = 0; y < _height; y++)
            Buffer.MemoryCopy(source + (long)y * _stride, destination + (long)y * target.RowBytes, target.RowBytes, rowBytes);

        // A cheap look at the picture: the average of 64 points. A frame that
        // goes dark right after a bright one is counted, to catch a black
        // frame when the loop starts over.
        long sum = 0;
        for (var i = 0; i < 64; i++)
        {
            var x = (i % 8 * 2 + 1) * _width / 16;
            var y = (i / 8 * 2 + 1) * _height / 16;
            var p = source + (long)y * _stride + x * 4;
            sum += p[0] + p[1] + p[2];
        }

        var brightness = sum / (64.0 * 3);
        if (_lastBrightness > 30 && brightness < 8)
        {
            _darkDrops++;
            Log.Warning("Video: cuadro casi negro después de uno claro (brillo {Before:0} → {After:0})",
                _lastBrightness, brightness);
        }

        _lastBrightness = brightness;
    }

    private void WatchLoop()
    {
        var time = _player.Time;
        if (_lastTime > 1000 && time >= 0 && time + 500 < _lastTime)
        {
            _loops++;
            Log.Information("Video: volvió a empezar (vuelta {Loop}, iba en {Before} ms)", _loops, _lastTime);
        }

        if (time >= 0)
            _lastTime = time;
    }

    private void Report()
    {
        var now = _clock.Elapsed;
        var seconds = Math.Max(0.001, (now - _lastReport).TotalSeconds);
        var delivered = _delivered - _lastDelivered;
        var painted = _painted - _lastPainted;
        var dropped = _dropped - _lastDropped;
        var memory = Process.GetCurrentProcess().WorkingSet64;

        Log.Information(
            "Video: {Painted:0.0} cuadros pintados/s, {Delivered:0.0} entregados/s, {Dropped} descartados ({DroppedPct:0.0} %) " +
            "| total pintados {TotalPainted}, descartados {TotalDropped} ({TotalPct:0.00} %), vueltas {Loops}, caídas a negro {Dark} " +
            "| memoria {Memory} MB ({Delta:+0;-0} MB desde que empezó), .NET {Managed} MB, video {Width}×{Height}, formatos {Formats}",
            painted / seconds, delivered / seconds, dropped, delivered == 0 ? 0 : 100.0 * dropped / delivered,
            _painted, _dropped, _delivered == 0 ? 0 : 100.0 * _dropped / _delivered, _loops, _darkDrops,
            memory / 1048576, (memory - _startMemory) / 1048576, GC.GetTotalMemory(false) / 1048576, _width, _height,
            Interlocked.Read(ref _formats));

        _lastReport = now;
        _lastDelivered = _delivered;
        _lastPainted = _painted;
        _lastDropped = _dropped;
    }
}
