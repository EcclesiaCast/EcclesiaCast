using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.Presentation.Services;
using LibVLCSharp.Shared;
using Serilog;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// Plays a video into a <see cref="WriteableBitmap"/> with LibVLC's video
/// callbacks, so the slide is an ordinary layer on top: the same control as
/// the WPF <c>VlcVideoSurface</c>, with the same abilities (the loop with no
/// black frame, what to do at the end, pause, seek and skip, the small copy
/// for the operator's preview, the blurred copy in the bars, the sound of its
/// own) and the same rules, learned the hard way there and in the prototype:
/// • ONE player for the control's lifetime, with the callback delegates kept
///   in fields. A collected delegate the native side still calls kills the
///   process with no managed exception.
/// • The format callback never waits for the UI thread. VLC calls it several
///   times per video (1088 then 1090 high), so the bitmap is made lazily on
///   the UI thread instead.
/// • The frame is copied into the bitmap on the UI thread; VLC's thread only
///   fills the buffer, shrinks the small copies and posts.
///
/// What WPF has and this does not: nothing of its own. The WPF surface uses
/// no <c>D3DImage</c>, and «bajo rendimiento» reaches it only as
/// <see cref="HoldFade"/> = 0, which whoever owns the surface sets here too.
/// Avalonia does not tell an <see cref="Image"/> that the pixels of its
/// bitmap changed, so every view that shows a picture of this surface is
/// redrawn by hand (<see cref="FrameWritten"/>, <see cref="PreviewFrameWritten"/>).
/// </summary>
public sealed class VlcVideoSurface : Panel
{
    private const int Alignment = 64;

    private readonly object _sync = new();

    private readonly Image _backdrop;
    private readonly Image _surface;
    private readonly Image _hold;

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
    /// stayed frozen over the new image.
    /// </summary>
    private int _generation;

    // ── Cuadro achicado para el panel LIVE del operador ──────────
    private static readonly long PreviewInterval = System.Diagnostics.Stopwatch.Frequency / VideoPreviewFrame.PerSecond;
    private readonly System.Diagnostics.Stopwatch _previewClock = System.Diagnostics.Stopwatch.StartNew();
    private long _lastPreviewTicks = long.MinValue / 2;
    private uint[]? _previewPixels;
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
        // «Rellenar» draws the picture whole and lets what does not fit stick
        // out: the edge of the screen is the only crop.
        ClipToBounds = true;

        // «El mismo video, desenfocado»: a tiny copy of each frame, already
        // blurred, stretched over the whole screen behind the framed video.
        // The smooth stretch finishes the blur.
        _backdrop = new Image { Stretch = Stretch.UniformToFill, IsVisible = false, IsHitTestVisible = false };
        RenderOptions.SetBitmapInterpolationMode(_backdrop, BitmapInterpolationMode.MediumQuality);
        _surface = new Image { Stretch = Stretch.UniformToFill };

        // The last picture of the video before, frozen, while the first of the
        // next arrives (or while an image fades in over it).
        _hold = new Image { Stretch = Stretch.UniformToFill, IsVisible = false, IsHitTestVisible = false };

        Children.Add(_backdrop);
        Children.Add(_surface);
        Children.Add(_hold);

        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _holdTimer.Tick += (_, _) =>
        {
            // The next picture never came (VLC failed to open it).
            _holdTimer.Stop();
            ReleaseHold(HoldFade);
        };

        AttachedToVisualTree += (_, _) =>
        {
            AudioOutputChanges.OutputChanged -= OnAudioOutputChanged;
            AudioOutputChanges.OutputChanged += OnAudioOutputChanged;
        };
        DetachedFromVisualTree += (_, _) => Dispose();
    }

    /// <summary>The engine every player shares; null leaves the video off.</summary>
    private static LibVLC? Engine => PlatformServices.Video?.Instance;

    /// <summary>The picture the frames are drawn in (for the tests).</summary>
    internal Image Surface => _surface;

    /// <summary>The frozen last picture (for the tests).</summary>
    internal Image Hold => _hold;

    /// <summary>The blurred copy in the bars (for the tests).</summary>
    internal Image Backdrop => _backdrop;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        // The framing is given over a 1920×1080 canvas: a different size means
        // different numbers, and a different shape may make bars appear or go.
        if (finalSize != _arrangedSize)
        {
            _arrangedSize = finalSize;
            MediaFraming.Apply(_surface, _framing, finalSize.Width, finalSize.Height);
            UpdateBackdrop();

            // A held picture keeps the framing it was taken with.
            if (_hold.IsVisible && _hold.Source is not null)
                MediaFraming.Apply(_hold, _holdFraming, finalSize.Width, finalSize.Height);
        }

        return arranged;
    }

    private Size _arrangedSize;

    private double ActualWidth => Bounds.Width;
    private double ActualHeight => Bounds.Height;

    /// <summary>Raised when a non-looping video reaches its end.</summary>
    public event EventHandler? Ended;

    /// <summary>
    /// Raised when the small copy of the picture for the operator's preview
    /// is replaced (a new video, a new size) or cleared. Nothing is shrunk
    /// while no one listens.
    /// </summary>
    public event EventHandler<IImage?>? PreviewFrameChanged;

    /// <summary>Raised on the UI thread every time new pixels were written into the small copy.</summary>
    public event EventHandler? PreviewFrameWritten;

    /// <summary>Raised on the UI thread every time a frame was written into <see cref="FrameSourceChanged"/>'s bitmap.</summary>
    public event EventHandler? FrameWritten;

    /// <summary>How many frames were drawn since the surface was made (for the tests and the log).</summary>
    public long FramesDrawn { get; private set; }

    private long _framesDropped;

    /// <summary>
    /// How many frames VLC handed over while the one before was still being
    /// drawn, and were let go (for the log: the Mac's «less than 1 % lost»).
    /// </summary>
    public long FramesDropped => Interlocked.Read(ref _framesDropped);

    /// <summary>Shows/loops the given video, or stops if it's not a video.</summary>
    public void Show(MediaItem? media)
    {
        if (media is not { Type: MediaType.Video } || Engine is not { } engine)
        {
            Stop();
            return;
        }

        var player = EnsurePlayer(engine);
        if (player is null)
            return;

        // Its own volume, never the shared one (see PlayerSound).
        PlayerSound.KeepSessionFull(player);
        var volume = PlayerSound.Effective(media.Volume);
        if (volume != _appliedVolume)
        {
            PlayerSound.SetVolume(player, media.Volume);
            _appliedVolume = volume;
        }

        // Re-applying the same background must not restart it — the operator
        // may have paused it on purpose, and changing slides re-applies it.
        // Its framing may have changed, though, and that is redrawn in place.
        if (media.Id == _currentId)
        {
            _framing = media;
            _playing = media;
            ApplyStretch();
            UpdateSilence(engine, player, media);
            return;
        }

        // The last picture of the old video stays on screen, frozen, until the
        // first one of the new video arrives: the gap in between used to show
        // as a flash of black.
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
        var playable = LightVideoCache.PlayablePath(media);
        using var m = new Media(engine, new Uri(playable));
        var loops = media.EndBehavior == VideoEndBehavior.Loop;
        if (loops)
            m.AddOption(":input-repeat=65535");
        m.AddOption(":file-caching=500");

        // A silenced video is opened without its sound.
        var silent = IsSilent(media);
        if (silent)
            m.AddOption(":no-audio");
        _openedSilent = silent;
        _trackOff = false;
        _playingName = media.Name;

        // Trimming is handed to VLC: a loop then restarts at the trimmed start.
        // Reopening in place picks up where it was, except a loop.
        if (resumeAt is { } at && !loops && at > TimeSpan.Zero)
            m.AddOption($":start-time={at.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)}");
        else if (media.TrimStart > 0)
            m.AddOption($":start-time={media.TrimStart.ToString("0.###", CultureInfo.InvariantCulture)}");
        if (media.TrimEnd > 0 && media.TrimEnd > media.TrimStart)
            m.AddOption($":stop-time={media.TrimEnd.ToString("0.###", CultureInfo.InvariantCulture)}");

        // With hardware decoding on, handing the player a new input while the
        // old one is still decoding corrupts the native heap: stop first. Safe
        // to block here: no callback waits on the UI thread.
        player.Stop();
        Interlocked.Increment(ref _generation);

        // The system changed the audio device since this player opened its
        // output: a fresh one now that it is stopped. The player itself is
        // never rebuilt.
        if (_audioGeneration != AudioOutputChanges.Generation)
        {
            _audioGeneration = AudioOutputChanges.Generation;
            try
            {
                var ok = player.SetAudioOutput(AudioOutputName);
                PlayerSound.KeepSessionFull(player);
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

        var sound = silent ? "sin sonido" : $"con sonido al {Math.Clamp(media.Volume, 0, 100)} %";
        if (playable == media.Path)
            Log.Information("Video de fondo: {Name} ({Path}), {Sound}", media.Name, media.Path, sound);
        else
            Log.Information("Video de fondo: {Name} ({Path}) desde su copia liviana {Copy}, {Sound}",
                media.Name, media.Path, playable, sound);
    }

    /// <summary>
    /// The audio output VLC opens: the system's default device of the moment
    /// (Windows' «mmdevice», macOS's Core Audio «auhal»).
    /// </summary>
    internal static string AudioOutputName => OperatingSystem.IsMacOS() ? "auhal" : "mmdevice";

    // ── Sonido propio del video ──────────────────────────────────

    private bool _openedSilent;
    private bool _trackOff;
    private int _savedTrack = -1;
    private string? _playingName;
    private int _appliedVolume = -1;

    private static bool IsSilent(MediaItem media) => media.Muted || media.Volume <= 0;

    /// <summary>
    /// The same video is put on again with its sound switched: silencing turns
    /// its sound track off in place; the sound comes back on that track, or by
    /// reopening it where it was when it was opened without one.
    /// </summary>
    private void UpdateSilence(LibVLC engine, MediaPlayer player, MediaItem media)
    {
        try
        {
            if (IsSilent(media))
            {
                if (!_openedSilent && !_trackOff)
                {
                    _savedTrack = player.AudioTrack;
                    player.SetAudioTrack(-1);
                    _trackOff = true;
                    Log.Information("Se silencia el video {Name}", media.Name);
                }
                return;
            }

            if (_trackOff && _savedTrack >= 0 && player.SetAudioTrack(_savedTrack))
            {
                _trackOff = false;
                Log.Information("Vuelve el sonido del video {Name}", media.Name);
                return;
            }

            if (!_openedSilent && !_trackOff)
                return;

            // Paused, it gets its sound the next time it is put on.
            if (!player.IsPlaying)
                return;

            var position = TimeSpan.FromMilliseconds(Math.Max(0, player.Time));
            Log.Information("Se vuelve a abrir el video {Name} con sonido en {Position}", media.Name, position);
            HoldLastFrame();
            StartPlayback(engine, player, media, position);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo cambiar el sonido del video");
        }
    }

    /// <summary>Leaves a line in the log when a video that should sound has no sound VLC can play.</summary>
    private void CheckSoundTrack(MediaPlayer player)
    {
        if (_openedSilent || _trackOff)
            return;

        var generation = Volatile.Read(ref _generation);
        var name = _playingName;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(1500);
            if (Volatile.Read(ref _generation) != generation || _openedSilent || _trackOff
                || !ReferenceEquals(_player, player))
                return;
            try
            {
                if (player.AudioTrackCount <= 0)
                    Log.Warning("El video {Name} no trae sonido que se pueda reproducir (ninguna pista de audio)", name);
                else
                    Log.Information("El video {Name} suena por la pista {Track} de {Count}",
                        name, player.AudioTrack, player.AudioTrackCount - 1);
            }
            catch { /* el reproductor ya se cerró */ }
        });
    }

    // ── Cambio de dispositivo de audio ───────────────────────────

    private MediaItem? _playing;
    private int _audioGeneration;

    /// <summary>
    /// The system moved the sound to another device: a video playing with
    /// sound is reopened on the new one, in the same place.
    /// </summary>
    private void OnAudioOutputChanged(object? sender, EventArgs e)
    {
        var player = _player;
        var media = _playing;
        if (player is null || media is null || _currentId < 0 || media.Muted
            || Engine is not { } engine || !player.IsPlaying)
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
    /// stays on screen, frozen, until <see cref="ReleaseHold"/>.
    /// </summary>
    public void Stop(bool holdLastFrame = false)
    {
        _currentId = -1;
        _pausedForHiddenOutput = false;
        _pausedForCover = false;
        _coverRetry?.Stop();

        if (holdLastFrame)
            HoldLastFrame();

        // A paused video counts too: left open it keeps its decoder alive.
        var player = _player;
        if (player is not null && player.State is not (VLCState.Stopped or VLCState.NothingSpecial))
        {
            try { player.Stop(); } catch { /* ignore */ }
        }

        Interlocked.Increment(ref _generation);
        if (holdLastFrame)
            Dispatcher.UIThread.Post(() => PreviewFrameChanged?.Invoke(this, null));
        else
            ClearSurface();
    }

    // ── Último cuadro retenido ───────────────────────────────────

    /// <summary>
    /// How long the held picture takes to fade away when the first frame of the
    /// next video arrives. Zero cuts («bajo rendimiento»).
    /// </summary>
    public TimeSpan HoldFade { get; set; } = TimeSpan.FromMilliseconds(320);

    /// <summary>Raised on the UI thread when a video draws its first picture.</summary>
    public event EventHandler? FirstFrame;

    /// <summary>
    /// The bitmap the frames are written into, raised with the first frame of
    /// every video put on. Whoever draws it elsewhere (the logo layer) sees the
    /// video move without a second player.
    /// </summary>
    public event EventHandler<IImage>? FrameSourceChanged;

    private int _announcedGeneration = -1;

    private readonly DispatcherTimer _holdTimer;
    private int _holdVersion;
    private double _blurRadius;
    private MediaItem? _holdFraming;

    /// <summary>True while the last picture of a video is held on screen.</summary>
    public bool IsHolding => _hold.IsVisible;

    /// <summary>
    /// Freezes a copy of the picture on screen into the hold layer and empties
    /// the live surface. Does nothing when no picture was drawn yet.
    /// </summary>
    private void HoldLastFrame()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(HoldLastFrame);
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
            var copy = CopyOf(bitmap);

            _holdVersion++;
            Fades.Stop(_hold, OpacityProperty);
            _hold.Opacity = 1;
            _hold.Source = copy;
            _holdFraming = _framing;
            MediaFraming.Apply(_hold, _holdFraming, ActualWidth, ActualHeight);
            MediaFraming.SetBlur(_hold, _blurRadius);
            _hold.IsVisible = true;
            StartHoldTimer();
        }
        catch (Exception ex)
        {
            // Without the copy the change simply goes through black.
            Log.Debug(ex, "No se pudo retener el último cuadro del video");
        }

        lock (_sync)
        {
            _surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            _previewBitmap = null;
        }
    }

    private static unsafe WriteableBitmap CopyOf(WriteableBitmap bitmap)
    {
        var copy = new WriteableBitmap(bitmap.PixelSize, bitmap.Dpi, PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using var from = bitmap.Lock();
        using var to = copy.Lock();
        var rowBytes = Math.Min(from.RowBytes, to.RowBytes);
        for (var y = 0; y < bitmap.PixelSize.Height; y++)
            Buffer.MemoryCopy((byte*)from.Address + (long)y * from.RowBytes, (byte*)to.Address + (long)y * to.RowBytes,
                to.RowBytes, rowBytes);
        return copy;
    }

    /// <summary>
    /// Lets go of the held picture, fading it out over <paramref name="fade"/>
    /// (zero cuts). Called when whatever replaces it is already on screen; if
    /// that never happens, the hold timer lets go anyway.
    /// </summary>
    public void ReleaseHold(TimeSpan fade)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ReleaseHold(fade));
            return;
        }

        _holdTimer.Stop();
        if (!_hold.IsVisible)
            return;

        if (fade <= TimeSpan.Zero)
        {
            ClearHold();
            return;
        }

        var version = ++_holdVersion;
        FadeOutHold(version, fade);

        // No video follows: its blurred bars fade out with its last picture.
        if (_currentId < 0 && _backdrop.IsVisible)
            _ = Fades.Fade(_backdrop, 0, fade);
    }

    private async void FadeOutHold(int version, TimeSpan fade)
    {
        await Fades.Fade(_hold, 1, 0, fade);
        if (version == _holdVersion)
            ClearHold();
    }

    private void ClearHold()
    {
        _holdTimer.Stop();
        _holdVersion++;
        Fades.Stop(_hold, OpacityProperty);
        _hold.Opacity = 1;
        _hold.IsVisible = false;
        _hold.Source = null;

        if (_currentId < 0)
            HideBackdrop();
    }

    private void StartHoldTimer()
    {
        _holdTimer.Interval = VideoHold.Timeout(HoldFade);
        _holdTimer.Stop();
        _holdTimer.Start();
    }

    // ── Transporte (barra de reproducción del operador) ──────────

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

    private bool _covered;
    private bool _pausedForCover;
    private DispatcherTimer? _coverRetry;
    private int _coverTries;

    /// <summary>
    /// A video nobody can see does not have to be decoded and drawn: with the
    /// logo up it is paused, and it picks up where it was when the logo goes.
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
        if (_coverRetry is null)
        {
            _coverRetry = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _coverRetry.Tick += OnCoverRetry;
        }
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
        MediaFraming.SetBlur(_surface, radius);
        if (_hold.IsVisible)
            MediaFraming.SetBlur(_hold, radius);
    }

    private void OnMasterChanged(object? sender, EventArgs e)
    {
        if (_player is { } player)
            PlayerSound.KeepSessionFull(player);
    }

    private MediaPlayer? EnsurePlayer(LibVLC engine)
    {
        if (_player is not null)
            return _player;

        try
        {
            var player = new MediaPlayer(engine)
            {
                // The graphics card decodes and VLC copies each frame back
                // (VideoToolbox on a Mac). Only safe because Play() always
                // stops the previous input first.
                EnableHardwareDecoding = true,
            };

            _formatCb = OnFormat;
            _cleanupCb = OnCleanup;
            _lockCb = OnLock;
            _displayCb = OnDisplay;
            player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
            player.SetVideoCallbacks(_lockCb, null, _displayCb);

            player.EncounteredError += (_, _) => Log.Error("VLC no pudo reproducir el video de fondo");
            player.Playing += (_, _) => CheckSoundTrack(player);
            player.EndReached += (_, _) =>
                Dispatcher.UIThread.Post(() => Ended?.Invoke(this, EventArgs.Empty));

            _player = player;
            _appliedVolume = -1;
            PlayerSound.MasterChanged += OnMasterChanged;
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
        AudioOutputChanges.OutputChanged -= OnAudioOutputChanged;
        PlayerSound.MasterChanged -= OnMasterChanged;
        _holdTimer.Stop();
        _coverRetry?.Stop();
        ClearHold();
        _currentId = -1;

        var player = _player;
        _player = null;
        Interlocked.Increment(ref _generation);

        if (player is not null)
        {
            try { player.Stop(); } catch { /* ignore */ }
            try { player.Dispose(); } catch { /* ignore */ }
        }

        lock (_sync)
        {
            _surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            _previewBitmap = null;
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
    /// Makes the next preview frame announce a fresh picture: whoever started
    /// listening late would otherwise never receive it.
    /// </summary>
    public void ResetPreview()
    {
        lock (_sync)
            _previewBitmap = null;
    }

    private void ClearSurface() =>
        Dispatcher.UIThread.Post(() =>
        {
            ClearHold();
            _surface.Source = null;
            _bitmap = null;
            _bitmapWidth = _bitmapHeight = 0;
            _previewBitmap = null;
            PreviewFrameChanged?.Invoke(this, null);
        });

    /// <summary>Sizes and places the picture the way the operator framed it, on the UI thread.</summary>
    private void ApplyStretch() =>
        Dispatcher.UIThread.Post(() =>
        {
            MediaFraming.Apply(_surface, _framing, ActualWidth, ActualHeight);
            UpdateBackdrop();
        });

    // ── El mismo video, desenfocado, en los bordes ───────────────

    /// <summary>
    /// Set on the output's background video only: it draws a blurred copy of
    /// itself in the bars when its item asks for it.
    /// </summary>
    public bool DrawsOwnBackdrop { get; set; }

    private volatile bool _backdropWanted;
    private uint[]? _backdropPixels, _backdropScratch;
    private int _backdropWidth, _backdropHeight;
    private bool _backdropReady;
    private WriteableBitmap? _backdropBitmap;

    private void UpdateBackdrop()
    {
        if (!DrawsOwnBackdrop)
            return;

        if (_currentId < 0 || _framing is not { FillsWithOwnVideo: true })
        {
            _backdropWanted = false;
            HideBackdrop();
            return;
        }

        int width, height;
        lock (_sync)
        {
            width = _bitmapWidth;
            height = _bitmapHeight;
        }

        if (width <= 0 || height <= 0)
        {
            _backdropWanted = false;
            return;
        }

        var wanted = VideoBackdrop.ShowsBars(_framing, width, height, ActualWidth, ActualHeight);
        _backdropWanted = wanted;
        if (!wanted)
            HideBackdrop();
    }

    private void HideBackdrop()
    {
        Fades.Stop(_backdrop, OpacityProperty);
        _backdrop.Opacity = 1;
        _backdrop.IsVisible = false;
        _backdrop.Source = null;
        _backdropBitmap = null;
    }

    /// <summary>Shrinks and softens the frame into the small copy. VLC's thread, with <see cref="_sync"/> held.</summary>
    private unsafe void MakeBackdrop()
    {
        if (_buffer == IntPtr.Zero || _width <= 0 || _height <= 0)
            return;

        var (w, h) = VideoBackdrop.SmallSize(_width, _height);
        if (_backdropPixels is null || _backdropPixels.Length != w * h)
        {
            _backdropPixels = new uint[w * h];
            _backdropScratch = new uint[w * h];
        }

        _backdropWidth = w;
        _backdropHeight = h;
        var frame = new ReadOnlySpan<byte>((void*)_buffer, _stride * _height);
        VideoBackdrop.Shrink(frame, _width, _height, _stride, _backdropPixels, w, h);
        VideoBackdrop.Soften(_backdropPixels, w, h, _backdropScratch);
        _backdropReady = true;
    }

    /// <summary>Puts the small copy just made on screen. UI thread, with <see cref="_sync"/> held.</summary>
    private void ShowBackdrop()
    {
        _backdropReady = false;
        if (!_backdropWanted || _backdropPixels is null)
            return;

        if (_backdropBitmap is null
            || _backdropBitmap.PixelSize.Width != _backdropWidth
            || _backdropBitmap.PixelSize.Height != _backdropHeight)
        {
            _backdropBitmap = NewBitmap(_backdropWidth, _backdropHeight);
            _backdrop.Source = _backdropBitmap;
        }

        WritePixels(_backdropBitmap, _backdropPixels, _backdropWidth, _backdropHeight);
        _backdrop.InvalidateVisual();

        // A video put on while the bars of the one before were fading away.
        Fades.Stop(_backdrop, OpacityProperty);
        _backdrop.Opacity = 1;
        _backdrop.IsVisible = true;
    }

    private static WriteableBitmap NewBitmap(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);

    private static unsafe void WritePixels(WriteableBitmap bitmap, uint[] pixels, int width, int height)
    {
        using var target = bitmap.Lock();
        fixed (uint* source = pixels)
        {
            for (var y = 0; y < height; y++)
                Buffer.MemoryCopy(source + (long)y * width, (byte*)target.Address + (long)y * target.RowBytes,
                    target.RowBytes, width * 4);
        }
    }

    // ── Callbacks de VLC (hilo de decodificación) ────────────────

    private uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        try
        {
            // Asking VLC for a smaller picture was measured and dropped on
            // Windows: its scaler cost more than drawing the full frame.
            var w = (int)width;
            var h = (int)height;

            // Aligned pitch: VLC fills frames with SIMD writes.
            var stride = (w * 4 + Alignment - 1) & ~(Alignment - 1);
            var needed = stride * h;

            // RV32: B, G, R, X in memory — Avalonia's Bgra8888.
            Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);
            pitches = (uint)stride;
            lines = (uint)h;

            lock (_sync)
            {
                // The buffer only ever grows, so a frame in flight can never
                // point at memory just freed.
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
        {
            Interlocked.Increment(ref _framesDropped);
            return;
        }

        var generation = Volatile.Read(ref _generation);

        // The shrinking happens here, on VLC's thread.
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

        if (_backdropWanted)
        {
            lock (_sync)
                MakeBackdrop();
        }

        Dispatcher.UIThread.Post(() => Paint(generation), DispatcherPriority.Render);
    }

    // ── Hilo de la pantalla ──────────────────────────────────────

    private void Paint(int generation)
    {
        var firstFrame = false;
        var previewWritten = false;
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
                    _bitmap = NewBitmap(_width, _height);
                    _bitmapWidth = _width;
                    _bitmapHeight = _height;
                    _surface.Source = _bitmap;
                    firstFrame = true;

                    // «Rellenar» depends on the shape of the picture, known now.
                    MediaFraming.Apply(_surface, _framing, ActualWidth, ActualHeight);
                }

                CopyFrame(_bitmap);

                if (_backdropReady)
                    ShowBackdrop();

                if (_previewReady)
                {
                    _previewReady = false;
                    if (_previewBitmap is null
                        || _previewBitmap.PixelSize.Width != _previewWidth
                        || _previewBitmap.PixelSize.Height != _previewHeight)
                    {
                        _previewBitmap = NewBitmap(_previewWidth, _previewHeight);
                        PreviewFrameChanged?.Invoke(this, _previewBitmap);
                    }

                    WritePixels(_previewBitmap, _previewPixels!, _previewWidth, _previewHeight);
                    previewWritten = true;
                }
            }

            FramesDrawn++;
            _surface.InvalidateVisual();
            FrameWritten?.Invoke(this, EventArgs.Empty);
            if (previewWritten)
                PreviewFrameWritten?.Invoke(this, EventArgs.Empty);

            // The new picture is drawn: the old one held in front of it can
            // fade away now, and so can whatever else was kept for it.
            if ((firstFrame || generation != _announcedGeneration) && _bitmap is { } made)
            {
                _announcedGeneration = generation;
                FrameSourceChanged?.Invoke(this, made);
            }

            if (firstFrame)
            {
                UpdateBackdrop();
                ReleaseHold(HoldFade);
                FirstFrame?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            // Frame skipped; the next one will draw.
            Log.Debug(ex, "Cuadro salteado");
        }
        finally
        {
            Interlocked.Exchange(ref _framePending, 0);
        }
    }

    /// <summary>Copies the frame VLC left in the buffer into the bitmap. UI thread, with <see cref="_sync"/> held.</summary>
    private unsafe void CopyFrame(WriteableBitmap bitmap)
    {
        using var target = bitmap.Lock();
        var rowBytes = _width * 4;
        var source = (byte*)_buffer;
        var destination = (byte*)target.Address;
        if (target.RowBytes == _stride)
        {
            Buffer.MemoryCopy(source, destination, (long)target.RowBytes * _height, (long)_stride * _height);
            return;
        }

        for (var y = 0; y < _height; y++)
            Buffer.MemoryCopy(source + (long)y * _stride, destination + (long)y * target.RowBytes, target.RowBytes, rowBytes);
    }

    /// <summary>Copies the current frame into the preview's small buffer. Call with <see cref="_sync"/> held.</summary>
    private unsafe void ShrinkForPreview()
    {
        if (_buffer == IntPtr.Zero || _width <= 0 || _height <= 0)
            return;

        var (step, pw, ph) = VideoPreviewFrame.SizeFor(_width, _height);
        if (_previewPixels is null || _previewPixels.Length != pw * ph)
            _previewPixels = new uint[pw * ph];
        _previewWidth = pw;
        _previewHeight = ph;

        var frame = new ReadOnlySpan<byte>((void*)_buffer, _stride * _height);
        VideoPreviewFrame.Shrink(frame, _stride, step, _previewPixels, pw, ph);
        _previewReady = true;
    }
}
