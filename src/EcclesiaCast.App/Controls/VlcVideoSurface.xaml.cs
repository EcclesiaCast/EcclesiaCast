using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EcclesiaCast.Core.Media;
using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Plays a looping video into a WriteableBitmap using LibVLC's video
/// callbacks, so WPF text overlays it without airspace issues. All buffer
/// access is guarded so freeing memory can never race the native decode
/// thread.
/// </summary>
public partial class VlcVideoSurface : UserControl
{
    private readonly object _sync = new();

    private MediaPlayer? _player;
    private IntPtr _buffer;
    private int _width, _height, _stride;
    private WriteableBitmap? _bitmap;
    private MediaScaling _scaling = MediaScaling.Fill;
    private int _currentId = -1;

    /// <summary>1 while a frame copy is queued on the UI thread — newer frames are dropped, never queued.</summary>
    private int _framePending;

    // Keep delegates alive for the native side.
    private MediaPlayer.LibVLCVideoFormatCb? _formatCb;
    private MediaPlayer.LibVLCVideoCleanupCb? _cleanupCb;
    private MediaPlayer.LibVLCVideoLockCb? _lockCb;
    private MediaPlayer.LibVLCVideoDisplayCb? _displayCb;

    public VlcVideoSurface()
    {
        InitializeComponent();
        Unloaded += (_, _) => Stop();
    }

    /// <summary>Shows/loops the given video, or stops if it's not a video.</summary>
    public void Show(MediaItem? media)
    {
        if (media is not { Type: MediaType.Video } || App.VideoEngine is not { } engine)
        {
            Stop();
            return;
        }

        if (media.Id == _currentId && _player is not null)
        {
            _player.Mute = media.Muted;
            _player.Volume = Math.Clamp(media.Volume, 0, 100);
            return;
        }

        Stop();
        _currentId = media.Id;
        _scaling = media.Scaling;

        var player = new MediaPlayer(engine)
        {
            Mute = media.Muted,
            // Copy-back HW decoding: the GPU decodes, we still get the frames
            // in memory for the WriteableBitmap. Falls back to software
            // automatically if unsupported.
            EnableHardwareDecoding = true,
        };
        player.Volume = Math.Clamp(media.Volume, 0, 100);

        _formatCb = OnFormat;
        _cleanupCb = OnCleanup;
        _lockCb = OnLock;
        _displayCb = OnDisplay;
        player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
        player.SetVideoCallbacks(_lockCb, null, _displayCb);

        _player = player;

        try
        {
            using var m = new Media(engine, new Uri(media.Path));
            if (media.EndBehavior == VideoEndBehavior.Loop)
                m.AddOption(":input-repeat=65535");
            // Generous file cache so the loop restart has the data ready.
            m.AddOption(":file-caching=1500");
            player.Play(m);
        }
        catch
        {
            Stop();
        }
    }

    public void Stop()
    {
        _currentId = -1;

        MediaPlayer? player;
        lock (_sync)
        {
            player = _player;
            _player = null;
        }

        if (player is not null)
        {
            // Stop() blocks until the native callbacks stop; only then is it
            // safe to free the buffer. Do it off the UI thread to avoid a
            // deadlock with the format callback's Dispatcher.Invoke.
            System.Threading.Tasks.Task.Run(() =>
            {
                try { player.Stop(); } catch { /* ignore */ }
                try { player.Dispose(); } catch { /* ignore */ }
                lock (_sync)
                {
                    if (_buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_buffer);
                        _buffer = IntPtr.Zero;
                    }
                }
            });
        }

        Dispatcher.BeginInvoke(() =>
        {
            Surface.Source = null;
            _bitmap = null;
        });
    }

    // ── Callbacks de VLC (hilo de decodificación) ────────────────

    private uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        try
        {
            var w = (int)width;
            var h = (int)height;

            Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);
            var stride = w * 4;
            pitches = (uint)stride;
            lines = (uint)h;

            lock (_sync)
            {
                if (_buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(_buffer);
                _buffer = Marshal.AllocHGlobal(stride * h);
                _width = w;
                _height = h;
                _stride = stride;
            }

            Dispatcher.Invoke(() =>
            {
                _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgr32, null);
                Surface.Source = _bitmap;
                Surface.Stretch = _scaling switch
                {
                    MediaScaling.Fit => Stretch.Uniform,
                    MediaScaling.Stretch => Stretch.Fill,
                    _ => Stretch.UniformToFill,
                };
            });

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    private void OnCleanup(ref IntPtr opaque)
    {
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
        // Backpressure: if the UI is still copying the previous frame, drop
        // this one instead of queueing — queue build-up is what causes
        // visible stutter.
        if (System.Threading.Interlocked.CompareExchange(ref _framePending, 1, 0) != 0)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            try
            {
                lock (_sync)
                {
                    if (_bitmap is null || _buffer == IntPtr.Zero)
                        return;
                    _bitmap.Lock();
                    _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _buffer, _stride * _height, _stride);
                    _bitmap.Unlock();
                }
            }
            catch
            {
                // Frame skipped; next one will draw.
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _framePending, 0);
            }
        });
    }
}
