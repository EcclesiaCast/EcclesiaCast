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

    /// <summary>
    /// Bumped every time the input changes or stops. A frame queued for the
    /// UI thread before that carries the old number and is dropped: without
    /// it, the last frame of a video landed AFTER the surface was cleared and
    /// stayed frozen in the operator's Live panel over the new image.
    /// </summary>
    private int _generation;

    // ── Cuadro achicado para el panel LIVE del operador ──────────
    // The Live panel is a box about 300 px wide. Handing it the projector's
    // full 1920×1080 bitmap made the operator's window upload ~8 MB to the
    // graphics card on every frame just to throw most of it away; on a
    // modest PC on the balanced power plan that was what made it stutter.
    // It now gets its own small copy, a few times a second.
    private const int PreviewTargetWidth = 480;
    private static readonly long PreviewInterval = System.Diagnostics.Stopwatch.Frequency / 15;
    private readonly System.Diagnostics.Stopwatch _previewClock = System.Diagnostics.Stopwatch.StartNew();
    private long _lastPreviewTicks = long.MinValue / 2;
    private int[]? _previewPixels;
    private int _previewWidth, _previewHeight;
    private bool _previewReady;
    private WriteableBitmap? _previewBitmap;

    // Assigned once and kept alive for as long as the player exists.
    private MediaPlayer.LibVLCVideoFormatCb? _formatCb;
    private MediaPlayer.LibVLCVideoCleanupCb? _cleanupCb;
    private MediaPlayer.LibVLCVideoLockCb? _lockCb;
    private MediaPlayer.LibVLCVideoDisplayCb? _displayCb;

    public VlcVideoSurface()
    {
        InitializeComponent();
        Unloaded += (_, _) => Dispose();
        Loaded += (_, _) =>
        {
            AudioEndpointWatcher.OutputChanged -= OnAudioOutputChanged;
            AudioEndpointWatcher.OutputChanged += OnAudioOutputChanged;
        };

        // The framing is given over a 1920×1080 canvas: a different window
        // size means different numbers.
        SizeChanged += (_, _) => MediaFraming.Apply(Surface, _framing, ActualWidth, ActualHeight);

        // A held picture keeps the framing it was taken with, so it only needs
        // the new size (it never lives long enough for anything else).
        SizeChanged += (_, _) =>
        {
            if (Hold.Visibility == Visibility.Visible && Hold.Source is not null)
                MediaFraming.Apply(Hold, _holdFraming, ActualWidth, ActualHeight);
        };
    }

    /// <summary>Raised when a non-looping video reaches its end.</summary>
    public event EventHandler? Ended;

    /// <summary>
    /// Raised when the small copy of the picture for the operator's preview
    /// is replaced (a new video, a new size) or cleared. The preview listens
    /// so it can show the video moving without a second decoder. Nothing is
    /// shrunk while no one listens.
    /// </summary>
    public event EventHandler<ImageSource?>? PreviewFrameChanged;

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
        // Its framing may have changed, though (edited in Propiedades while on
        // screen), and that is redrawn in place.
        if (media.Id == _currentId)
        {
            _framing = media;

            // The mute just applied above must be the one a change of sound
            // device looks at, or lifting the mute never reopens the sound.
            _playing = media;
            ApplyStretch();
            return;
        }

        // The last picture of the old video stays on screen, frozen, until the
        // first one of the new video arrives: the gap in between (the decoder
        // opening the file) used to show as a flash of black. Taken before the
        // framing changes, so it keeps the old video's own framing.
        HoldLastFrame();

        _currentId = media.Id;
        _pausedForHiddenOutput = false;
        _pausedForCover = false;
        _framing = media;
        ApplyStretch();

        try
        {
            StartPlayback(engine, player, media, resumeAt: null);
            _playing = media;
            TryPauseForCover();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo reproducir el video {Path}", media.Path);
            _currentId = -1;
        }
    }

    /// <summary>
    /// Hands the player the video. Switching media happens on the SAME player:
    /// no player teardown and no delegate churn.
    /// </summary>
    private void StartPlayback(LibVLC engine, MediaPlayer player, MediaItem media, TimeSpan? resumeAt)
    {
        // A heavy video (4K, 60 frames) plays from its light copy when
        // one is ready; the item itself always keeps the original's path.
        var playable = LightVideoCache.PlayablePath(media);
        using var m = new Media(engine, new Uri(playable));
        var loops = media.EndBehavior == VideoEndBehavior.Loop;
        if (loops)
            m.AddOption(":input-repeat=65535");
        // Medio segundo de cache alcanza para un archivo local y deja
        // varios cientos de megas menos en juego que el segundo y medio
        // que traía: el video de fondo se lee del disco de la misma PC.
        m.AddOption(":file-caching=500");

        // Trimming is handed to VLC rather than watched for in code: a
        // loop then restarts at the trimmed start, which is the whole
        // point of cutting a sting off the front of a stock loop.
        // Reopening in place (the sound device changed) picks up at the spot
        // it was — except a loop, which would then restart from that spot
        // every time round; it starts over, which nobody notices in a loop.
        if (resumeAt is { } at && !loops && at > TimeSpan.Zero)
            m.AddOption($":start-time={at.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)}");
        else if (media.TrimStart > 0)
            m.AddOption($":start-time={media.TrimStart.ToString("0.###", CultureInfo.InvariantCulture)}");
        if (media.TrimEnd > 0 && media.TrimEnd > media.TrimStart)
            m.AddOption($":stop-time={media.TrimEnd.ToString("0.###", CultureInfo.InvariantCulture)}");

        // With hardware decoding on, handing the player a new input while
        // the old one is still decoding corrupts the native heap within a
        // few dozen switches (measured: 0xC0000374 every run). Stopping
        // first made it survive 264 switches in a row. Safe to block here:
        // no callback waits on the UI thread.
        player.Stop();
        // After Stop returns VLC calls nothing more for the old input, so
        // every frame of it still queued carries the old number.
        Interlocked.Increment(ref _generation);

        // Windows changed the audio device since this player opened its output:
        // ask for a fresh one (the default device of the moment) now that it is
        // stopped. The player itself is never rebuilt — its video callbacks
        // must stay assigned for as long as it lives.
        if (_audioGeneration != AudioEndpointWatcher.Generation)
        {
            _audioGeneration = AudioEndpointWatcher.Generation;
            try
            {
                var ok = player.SetAudioOutput("mmdevice");
                Log.Information("Se reabre la salida de audio del video ({Result})", ok ? "ok" : "no se pudo");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudo reabrir la salida de audio del video");
            }
        }

        player.Play(m);
        if (resumeAt is not null)
            return;

        if (playable == media.Path)
            Log.Information("Video de fondo: {Name} ({Path})", media.Name, media.Path);
        else
            Log.Information("Video de fondo: {Name} ({Path}) desde su copia liviana {Copy}",
                media.Name, media.Path, playable);
    }

    // ── Cambio de dispositivo de audio de Windows ────────────────

    /// <summary>The item playing now, to reopen it in place if the sound device changes.</summary>
    private MediaItem? _playing;

    /// <summary>The <see cref="AudioEndpointWatcher.Generation"/> this player's output was opened under.</summary>
    private int _audioGeneration;

    /// <summary>
    /// Windows moved the sound to another device (or the operator asked to
    /// restart it): a video that is playing with sound is reopened on the new
    /// one, in the same place. A muted one has nothing to fix; a paused one is
    /// fixed by the next time it is started.
    /// </summary>
    private void OnAudioOutputChanged(object? sender, EventArgs e)
    {
        var player = _player;
        var media = _playing;
        if (player is null || media is null || _currentId < 0 || media.Muted
            || App.VideoEngine is not { } engine || !player.IsPlaying)
            return;

        try
        {
            var position = TimeSpan.FromMilliseconds(Math.Max(0, player.Time));
            Log.Information("Se reabre la salida del audio del video {Name} en {Position}", media.Name, position);
            StartPlayback(engine, player, media, position);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo reabrir la salida de audio del video");
        }
    }

    /// <summary>
    /// Stops the video. With <paramref name="holdLastFrame"/> its last picture
    /// stays on screen, frozen, until <see cref="ReleaseHold"/> — for when
    /// something else is about to fade in over it, so the change does not go
    /// through black.
    /// </summary>
    public void Stop(bool holdLastFrame = false)
    {
        _currentId = -1;
        _pausedForHiddenOutput = false;
        _pausedForCover = false;
        _coverRetry?.Stop();

        if (holdLastFrame)
            HoldLastFrame();

        // A paused video counts too: left open, it kept its decoder and frame
        // buffers alive behind the image that replaced it.
        var player = _player;
        if (player is not null && player.State is not (VLCState.Stopped or VLCState.NothingSpecial))
        {
            // Safe to block here: no callback waits on the UI thread.
            try { player.Stop(); } catch { /* ignore */ }
        }

        // Frames already queued for the UI thread must not draw over the
        // clear (see _generation).
        Interlocked.Increment(ref _generation);
        if (holdLastFrame)
            Dispatcher.BeginInvoke(() => PreviewFrameChanged?.Invoke(this, null));
        else
            ClearSurface();
    }

    // ── Último cuadro retenido ───────────────────────────────────

    /// <summary>
    /// How long the held picture takes to fade away when the first frame of the
    /// next video arrives. Zero cuts (the «bajo rendimiento» mode).
    /// </summary>
    public TimeSpan HoldFade { get; set; } = TimeSpan.FromMilliseconds(320);

    /// <summary>Raised on the UI thread when a video draws its first picture.</summary>
    public event EventHandler? FirstFrame;

    private readonly DispatcherTimer _holdTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _holdTimerHooked;
    private int _holdVersion;
    private double _blurRadius;
    private MediaItem? _holdFraming;

    /// <summary>
    /// Freezes a copy of the picture on screen into <see cref="Hold"/> and
    /// empties the live surface. UI thread. Does nothing when no picture has
    /// been drawn yet (then there is nothing to hold, and whatever is already
    /// held stays).
    /// </summary>
    private void HoldLastFrame()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(HoldLastFrame);
            return;
        }

        WriteableBitmap? bitmap;
        lock (_sync)
            bitmap = _bitmap;

        if (bitmap is null)
            return;

        try
        {
            // Only the UI thread writes into the bitmap, and this is the UI
            // thread: the copy cannot catch it half drawn.
            var copy = new WriteableBitmap(bitmap);
            copy.Freeze();

            _holdVersion++;
            Hold.BeginAnimation(OpacityProperty, null);
            Hold.Opacity = 1;
            Hold.Source = copy;
            _holdFraming = _framing;
            MediaFraming.Apply(Hold, _holdFraming, ActualWidth, ActualHeight);
            MediaFraming.SetBlur(Hold, _blurRadius);
            Hold.Visibility = Visibility.Visible;
            StartHoldTimer();
        }
        catch (Exception ex)
        {
            // Without the copy the change simply goes through black, as before.
            Log.Debug(ex, "No se pudo retener el último cuadro del video");
        }

        lock (_sync)
        {
            Surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            _previewBitmap = null;
        }
    }

    /// <summary>
    /// Lets go of the held picture, fading it out over <paramref name="fade"/>
    /// (zero cuts). It is called when whatever replaces it is already on
    /// screen; if that never happens, two seconds later this lets go anyway.
    /// </summary>
    public void ReleaseHold(TimeSpan fade)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ReleaseHold(fade));
            return;
        }

        _holdTimer.Stop();
        if (Hold.Visibility != Visibility.Visible)
            return;

        if (fade <= TimeSpan.Zero)
        {
            ClearHold();
            return;
        }

        var version = ++_holdVersion;
        var animation = new System.Windows.Media.Animation.DoubleAnimation(0, fade);
        animation.Completed += (_, _) =>
        {
            if (version == _holdVersion)
                ClearHold();
        };
        Hold.BeginAnimation(OpacityProperty, animation);
    }

    private void ClearHold()
    {
        _holdTimer.Stop();
        _holdVersion++;
        Hold.BeginAnimation(OpacityProperty, null);
        Hold.Opacity = 1;
        Hold.Visibility = Visibility.Collapsed;
        Hold.Source = null;
    }

    private void StartHoldTimer()
    {
        if (!_holdTimerHooked)
        {
            _holdTimerHooked = true;
            _holdTimer.Tick += (_, _) =>
            {
                // The next picture never came (VLC failed to open it).
                _holdTimer.Stop();
                ReleaseHold(HoldFade);
            };
        }

        // The fail-safe has to outlast the fade of whatever replaces this
        // picture: with a longer fade configured, letting go at two seconds
        // would show what is underneath before the new one is fully drawn.
        _holdTimer.Interval = HoldTimeout(HoldFade);
        _holdTimer.Stop();
        _holdTimer.Start();
    }

    /// <summary>How long a held picture waits for its replacement: two seconds, or the fade and a margin if that is longer.</summary>
    internal static TimeSpan HoldTimeout(TimeSpan fade) =>
        TimeSpan.FromSeconds(Math.Max(2, fade.TotalSeconds + 1));

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


    // ── Tapado por el logo ───────────────────────────────────────

    /// <summary>True while something that hides this video (the logo) is on the screen.</summary>
    private bool _covered;

    /// <summary>Set when this control paused the video because it is covered, to resume only that.</summary>
    private bool _pausedForCover;

    private DispatcherTimer? _coverRetry;
    private int _coverTries;

    /// <summary>
    /// A video nobody can see does not have to be decoded and drawn: with the
    /// logo up it is paused, and it picks up where it was when the logo goes.
    /// A video that is still opening when the cover arrives (the operator chose
    /// another one under the logo) is paused as soon as it starts.
    /// </summary>
    public void SetCovered(bool covered)
    {
        _covered = covered;
        if (covered)
        {
            TryPauseForCover();
            return;
        }

        _coverRetry?.Stop();
        if (!_pausedForCover)
            return;

        _pausedForCover = false;
        Resume();
    }

    private void TryPauseForCover()
    {
        if (!_covered || _pausedForCover || _currentId < 0)
            return;

        if (_player?.IsPlaying == true)
        {
            _coverRetry?.Stop();
            _pausedForCover = true;
            Pause();
            return;
        }

        // Still opening: look again in a moment.
        _coverTries = 0;
        _coverRetry ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _coverRetry.Tick -= OnCoverRetry;
        _coverRetry.Tick += OnCoverRetry;
        _coverRetry.Start();
    }

    private void OnCoverRetry(object? sender, EventArgs e)
    {
        if (!_covered || _pausedForCover || _currentId < 0 || ++_coverTries > 20)
        {
            _coverRetry?.Stop();
            return;
        }

        TryPauseForCover();
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

        // Switched on again while the logo is up: it stays paused until the logo goes.
        if (_covered)
        {
            _pausedForCover = true;
            return;
        }

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
        _blurRadius = radius;
        // Through the framing, which knows how much it scales the picture.
        MediaFraming.SetBlur(Surface, radius);
        if (Hold.Visibility == Visibility.Visible)
            MediaFraming.SetBlur(Hold, radius);
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
        AudioEndpointWatcher.OutputChanged -= OnAudioOutputChanged;
        _holdTimer.Stop();
        ClearHold();

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

    /// <summary>
    /// Makes the next preview frame announce a fresh picture. Needed after the
    /// preview was switched off and on again: the existing bitmap is only
    /// handed out when it is created, so whoever started listening late would
    /// otherwise never receive it.
    /// </summary>
    public void ResetPreview()
    {
        lock (_sync)
            _previewBitmap = null;
    }

    private void ClearSurface() =>
        Dispatcher.BeginInvoke(() =>
        {
            ClearHold();
            Surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            _previewBitmap = null;
            PreviewFrameChanged?.Invoke(this, null);
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
            // Asking VLC for a smaller picture when the projector is smaller
            // was measured and dropped: its scaler made a 1920×1280 loop cost
            // 33–68 % of a core at 1024×768 against 18 % at full size.
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

        var generation = Volatile.Read(ref _generation);

        // The shrinking happens here, on VLC's thread, so the operator's
        // window only ever handles the small copy.
        if (PreviewFrameChanged is not null)
        {
            var now = _previewClock.ElapsedTicks;
            if (now - _lastPreviewTicks >= PreviewInterval)
            {
                _lastPreviewTicks = now;
                lock (_sync)
                    ShrinkForPreview();
            }
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            var firstFrame = false;
            try
            {
                lock (_sync)
                {
                    if (generation != _generation)
                    {
                        _previewReady = false;
                        return;
                    }

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
                        firstFrame = true;

                        // «Rellenar» depends on the shape of the picture, which
                        // is only known now (or has just changed).
                        MediaFraming.Apply(Surface, _framing, ActualWidth, ActualHeight);
                    }

                    _bitmap.WritePixels(
                        new Int32Rect(0, 0, _width, _height), _buffer, _stride * _height, _stride);

                    if (_previewReady)
                    {
                        _previewReady = false;
                        if (_previewBitmap is null
                            || _previewBitmap.PixelWidth != _previewWidth
                            || _previewBitmap.PixelHeight != _previewHeight)
                        {
                            _previewBitmap = new WriteableBitmap(
                                _previewWidth, _previewHeight, 96, 96, PixelFormats.Bgr32, null);
                            PreviewFrameChanged?.Invoke(this, _previewBitmap);
                        }

                        _previewBitmap.WritePixels(
                            new Int32Rect(0, 0, _previewWidth, _previewHeight),
                            _previewPixels!, _previewWidth * 4, 0);
                    }
                }

                // The new picture is drawn: the old one held in front of it
                // can fade away now, and so can whatever else was kept for it.
                if (firstFrame)
                {
                    ReleaseHold(HoldFade);
                    FirstFrame?.Invoke(this, EventArgs.Empty);
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

    /// <summary>
    /// Copies the current frame into the preview's small buffer, averaging
    /// each 2×2 block at a step that lands near <see cref="PreviewTargetWidth"/>.
    /// Call with <see cref="_sync"/> held.
    /// </summary>
    private unsafe void ShrinkForPreview()
    {
        if (_buffer == IntPtr.Zero || _width <= 0 || _height <= 0)
            return;

        var step = Math.Max(1, (int)Math.Round(_width / (double)PreviewTargetWidth));
        var pw = Math.Max(1, _width / step);
        var ph = Math.Max(1, _height / step);

        if (_previewPixels is null || _previewPixels.Length != pw * ph)
            _previewPixels = new int[pw * ph];
        _previewWidth = pw;
        _previewHeight = ph;

        // The second sample of each pair; 0 when the frame is so small it is
        // copied pixel for pixel.
        var dx = step > 1 ? 1 : 0;
        var dy = step > 1 ? _stride : 0;

        var src = (byte*)_buffer;
        fixed (int* dst = _previewPixels)
        {
            for (var y = 0; y < ph; y++)
            {
                var row = src + (long)y * step * _stride;
                var outRow = (uint*)dst + y * pw;
                for (var x = 0; x < pw; x++)
                {
                    var p = (uint*)(row + x * step * 4);
                    var a = p[0];
                    var b = p[dx];
                    var c = *(uint*)((byte*)p + dy);
                    var d = *(uint*)((byte*)p + dy + dx * 4);

                    // Average the four pixels channel by channel (B, G, R).
                    var lo = ((a & 0x00FF00FF) + (b & 0x00FF00FF) + (c & 0x00FF00FF) + (d & 0x00FF00FF)) >> 2;
                    var mid = ((a & 0x0000FF00) + (b & 0x0000FF00) + (c & 0x0000FF00) + (d & 0x0000FF00)) >> 2;
                    outRow[x] = (lo & 0x00FF00FF) | (mid & 0x0000FF00);
                }
            }
        }

        _previewReady = true;
    }
}
