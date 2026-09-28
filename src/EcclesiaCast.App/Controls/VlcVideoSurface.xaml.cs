using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using Serilog;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Plays a looping video into a WriteableBitmap using LibVLC's video
/// callbacks, so WPF text overlays it without airspace issues.
///
/// Two things matter for stability, both learned the hard way:
/// • ONE player for the control's lifetime, with the callback delegates
///   assigned once. Creating a player per video and reassigning the delegate
///   fields let the GC collect delegates the native side still called —
///   an access violation that killed the process with no managed exception.
/// • The format callback must never block on the UI thread. VLC calls it
///   several times per video (and with different heights, e.g. 1088 then
///   1090), so the bitmap is created lazily on the UI thread instead.
/// </summary>
public partial class VlcVideoSurface : UserControl
{
    private const int Alignment = 64;

    private readonly object _sync = new();

    private MediaPlayer? _player;
    private IntPtr _rawBuffer, _buffer;
    private int _bufferSize;
    private int _width, _height, _stride;

    private WriteableBitmap? _bitmap;
    private int _bitmapWidth, _bitmapHeight;

    /// <summary>The item being played, kept for its framing (zoom, size, offset).</summary>
    private MediaItem? _framing;
    private int _currentId = -1;
    private int _framePending;

    // Assigned once and kept alive for as long as the player exists.
    private MediaPlayer.LibVLCVideoFormatCb? _formatCb;
    private MediaPlayer.LibVLCVideoCleanupCb? _cleanupCb;
    private MediaPlayer.LibVLCVideoLockCb? _lockCb;
    private MediaPlayer.LibVLCVideoDisplayCb? _displayCb;

    public VlcVideoSurface()
    {
        InitializeComponent();
        Unloaded += (_, _) => Dispose();

        // The framing is given over a 1920×1080 canvas: a different window
        // size means different numbers.
        SizeChanged += (_, _) => MediaFraming.Apply(Surface, _framing, ActualWidth, ActualHeight);
    }

    /// <summary>Raised when a non-looping video reaches its end.</summary>
    public event EventHandler? Ended;

    /// <summary>
    /// Raised when the picture this surface draws into is replaced (a new
    /// video, a new size) or cleared. The operator's preview listens so it
    /// can draw the same frames without a second decoder.
    /// </summary>
    public event EventHandler<ImageSource?>? FrameSourceChanged;

    /// <summary>The picture being drawn right now, or null when nothing plays.</summary>
    public ImageSource? FrameSource => Surface.Source;

    /// <summary>Shows/loops the given video, or stops if it's not a video.</summary>
    public void Show(MediaItem? media)
    {
        if (media is not { Type: MediaType.Video } || App.VideoEngine is not { } engine)
        {
            Stop();
            return;
        }

        var player = EnsurePlayer(engine);
        if (player is null)
            return;

        player.Mute = media.Muted;
        player.Volume = Math.Clamp(media.Volume, 0, 100);

        // Re-applying the same background must not restart it — the operator
        // may have paused it on purpose, and changing slides re-applies it.
        if (media.Id == _currentId)
            return;

        _currentId = media.Id;
        _pausedForHiddenOutput = false;
        _framing = media;
        ApplyStretch();

        try
        {
            // Switching media on the SAME player: no player teardown and no
            // delegate churn.
            using var m = new Media(engine, new Uri(media.Path));
            if (media.EndBehavior == VideoEndBehavior.Loop)
                m.AddOption(":input-repeat=65535");
            // Medio segundo de cache alcanza para un archivo local y deja
            // varios cientos de megas menos en juego que el segundo y medio
            // que traía: el video de fondo se lee del disco de la misma PC.
            m.AddOption(":file-caching=500");

            // Trimming is handed to VLC rather than watched for in code: a
            // loop then restarts at the trimmed start, which is the whole
            // point of cutting a sting off the front of a stock loop.
            if (media.TrimStart > 0)
                m.AddOption($":start-time={media.TrimStart.ToString("0.###", CultureInfo.InvariantCulture)}");
            if (media.TrimEnd > 0 && media.TrimEnd > media.TrimStart)
                m.AddOption($":stop-time={media.TrimEnd.ToString("0.###", CultureInfo.InvariantCulture)}");

            // With hardware decoding on, handing the player a new input while
            // the old one is still decoding corrupts the native heap within a
            // few dozen switches (measured: 0xC0000374 every run). Stopping
            // first made it survive 264 switches in a row. Safe to block here:
            // no callback waits on the UI thread.
            player.Stop();
            player.Play(m);
            Log.Information("Video de fondo: {Name} ({Path})", media.Name, media.Path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo reproducir el video {Path}", media.Path);
            _currentId = -1;
        }
    }

    public void Stop()
    {
        _currentId = -1;
        _pausedForHiddenOutput = false;

        var player = _player;
        if (player is not null && player.IsPlaying)
        {
            // Safe to block here: no callback waits on the UI thread.
            try { player.Stop(); } catch { /* ignore */ }
        }

        ClearSurface();
    }

    // ── Transporte (barra de reproducción del operador) ──────────

    /// <summary>Set while the output was switched off with a video playing.</summary>
    private bool _pausedForHiddenOutput;

    public PlaybackState State
    {
        get
        {
            var player = _player;
            if (player is null || _currentId < 0)
                return PlaybackState.None;

            var length = player.Length;   // ms, −1 mientras VLC abre el archivo
            var time = player.Time;
            return new PlaybackState(
                HasVideo: true,
                IsPlaying: player.IsPlaying,
                Position: TimeSpan.FromMilliseconds(Math.Max(0, time)),
                Duration: TimeSpan.FromMilliseconds(Math.Max(0, length)));
        }
    }

    public void Pause() => SetPaused(true);

    public void Resume() => SetPaused(false);

    public void TogglePlayPause() => SetPaused(_player?.IsPlaying == true);

    private void SetPaused(bool paused)
    {
        var player = _player;
        if (player is null || _currentId < 0 || !player.CanPause)
            return;

        try { player.SetPause(paused); } catch { /* ignore */ }
    }

    /// <summary>Pauses because the output was switched off, remembering to resume later.</summary>
    public void PauseForHiddenOutput()
    {
        if (_player?.IsPlaying != true)
            return;

        _pausedForHiddenOutput = true;
        Pause();
    }

    /// <summary>Resumes only a video this control paused when the output went off.</summary>
    public void ResumeAfterHiddenOutput()
    {
        if (!_pausedForHiddenOutput)
            return;

        _pausedForHiddenOutput = false;
        Resume();
    }

    public void SeekTo(TimeSpan position)
    {
        var player = _player;
        if (player is null || _currentId < 0 || !player.IsSeekable)
            return;

        try { player.Time = (long)Math.Max(0, position.TotalMilliseconds); } catch { /* ignore */ }
    }

    public void Skip(TimeSpan delta)
    {
        var player = _player;
        if (player is null || _currentId < 0)
            return;

        var target = TimeSpan.FromMilliseconds(player.Time) + delta;
        var length = player.Length;
        if (length > 0)
            target = TimeSpan.FromMilliseconds(Math.Min(target.TotalMilliseconds, length - 500));

        SeekTo(target < TimeSpan.Zero ? TimeSpan.Zero : target);
    }

    /// <summary>Live blur over the moving picture, 0–100.</summary>
    public void SetBlur(double amount)
    {
        var radius = Math.Clamp(amount, 0, 100) * 0.6;
        Surface.Effect = radius <= 0
            ? null
            : new System.Windows.Media.Effects.BlurEffect
            {
                Radius = radius,
                KernelType = System.Windows.Media.Effects.KernelType.Gaussian,
                RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance,
            };
    }

    private MediaPlayer? EnsurePlayer(LibVLC engine)
    {
        if (_player is not null)
            return _player;

        try
        {
            var player = new MediaPlayer(engine)
            {
                Mute = true,
                // The graphics card decodes and VLC copies each frame back for
                // the callbacks. Measured on the church's HEVC 1920×1280 loops:
                // about a quarter of the CPU of decoding in software, which is
                // what kept playback smooth once Windows' power saver throttled
                // the processor. It is only safe because Play() always stops
                // the previous input first (see Show).
                EnableHardwareDecoding = true,
            };

            _formatCb = OnFormat;
            _cleanupCb = OnCleanup;
            _lockCb = OnLock;
            _displayCb = OnDisplay;
            player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
            player.SetVideoCallbacks(_lockCb, null, _displayCb);

            player.EncounteredError += (_, _) => Log.Error("VLC no pudo reproducir el video de fondo");
            player.EndReached += (_, _) =>
                Dispatcher.BeginInvoke(() => Ended?.Invoke(this, EventArgs.Empty));

            _player = player;
            return player;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo crear el reproductor de video");
            return null;
        }
    }

    private void Dispose()
    {
        var player = _player;
        _player = null;

        if (player is not null)
        {
            try { player.Stop(); } catch { /* ignore */ }
            try { player.Dispose(); } catch { /* ignore */ }
        }

        lock (_sync)
        {
            if (_rawBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_rawBuffer);
                _rawBuffer = IntPtr.Zero;
                _buffer = IntPtr.Zero;
                _bufferSize = 0;
            }
        }
    }

    private void ClearSurface() =>
        Dispatcher.BeginInvoke(() =>
        {
            Surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            FrameSourceChanged?.Invoke(this, null);
        });

    /// <summary>
    /// Sizes and places the picture the way the operator framed it. Runs on
    /// the UI thread: VLC calls in from its own decoding thread.
    /// </summary>
    private void ApplyStretch() =>
        Dispatcher.BeginInvoke(() =>
            MediaFraming.Apply(Surface, _framing, ActualWidth, ActualHeight));

    // ── Callbacks de VLC (hilo de decodificación) ────────────────

    private uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        try
        {
            var w = (int)width;
            var h = (int)height;

            // Aligned pitch: VLC fills frames with SIMD writes.
            var stride = (w * 4 + Alignment - 1) & ~(Alignment - 1);
            var needed = stride * h;

            Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);
            pitches = (uint)stride;
            lines = (uint)h;

            lock (_sync)
            {
                // The buffer only ever grows, so a frame in flight can never
                // point at memory we just freed.
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
        // The buffer is reused across formats and freed on Dispose.
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
        // Backpressure: drop frames instead of queueing them when the UI is
        // busy — a growing queue is what makes playback stutter.
        if (Interlocked.CompareExchange(ref _framePending, 1, 0) != 0)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            try
            {
                lock (_sync)
                {
                    if (_buffer == IntPtr.Zero || _width <= 0 || _height <= 0)
                        return;

                    // VLC can renegotiate the size mid-stream, so the bitmap
                    // follows whatever the current format is.
                    if (_bitmap is null || _bitmapWidth != _width || _bitmapHeight != _height)
                    {
                        _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgr32, null);
                        _bitmapWidth = _width;
                        _bitmapHeight = _height;
                        Surface.Source = _bitmap;

                        // The operator's Live panel draws this very bitmap, so
                        // it shows the video moving instead of a still poster
                        // without decoding anything twice.
                        FrameSourceChanged?.Invoke(this, _bitmap);
                    }

                    _bitmap.Lock();
                    _bitmap.WritePixels(
                        new Int32Rect(0, 0, _width, _height), _buffer, _stride * _height, _stride);
                    _bitmap.Unlock();
                }
            }
            catch
            {
                // Frame skipped; the next one will draw.
            }
            finally
            {
                Interlocked.Exchange(ref _framePending, 0);
            }
        });
    }
}
