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
/// callbacks, so WPF text overlays it without airspace issues.
/// </summary>
public partial class VlcVideoSurface : UserControl
{
    private MediaPlayer? _player;
    private IntPtr _buffer;
    private int _width, _height, _stride;
    private WriteableBitmap? _bitmap;
    private MediaScaling _scaling = MediaScaling.Fill;
    private int _currentId = -1;

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

        _player = new MediaPlayer(engine)
        {
            Mute = media.Muted,
            EnableHardwareDecoding = false,
        };
        _player.Volume = Math.Clamp(media.Volume, 0, 100);

        _formatCb = OnFormat;
        _cleanupCb = OnCleanup;
        _lockCb = OnLock;
        _displayCb = OnDisplay;
        _player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
        _player.SetVideoCallbacks(_lockCb, null, _displayCb);

        using var m = new Media(engine, new Uri(media.Path));
        if (media.EndBehavior == VideoEndBehavior.Loop)
            m.AddOption(":input-repeat=65535");
        _player.Play(m);
    }

    public void Stop()
    {
        _currentId = -1;
        if (_player is not null)
        {
            var player = _player;
            _player = null;
            System.Threading.Tasks.Task.Run(() =>
            {
                player.Stop();
                player.Dispose();
            });
        }

        Dispatcher.BeginInvoke(() =>
        {
            Surface.Source = null;
            _bitmap = null;
        });

        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
        }
    }

    // ── Callbacks de VLC (hilo de decodificación) ────────────────

    private uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        var w = (int)width;
        var h = (int)height;
        _width = w;
        _height = h;
        _stride = w * 4;

        Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);
        pitches = (uint)_stride;
        lines = (uint)h;

        if (_buffer != IntPtr.Zero)
            Marshal.FreeHGlobal(_buffer);
        _buffer = Marshal.AllocHGlobal(_stride * h);

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

    private void OnCleanup(ref IntPtr opaque)
    {
    }

    private IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        Marshal.WriteIntPtr(planes, _buffer);
        return _buffer;
    }

    private void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        var buffer = _buffer;
        var w = _width;
        var h = _height;
        var stride = _stride;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            if (_bitmap is null || buffer == IntPtr.Zero)
                return;
            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, w, h), buffer, stride * h, stride);
            _bitmap.Unlock();
        });
    }
}
