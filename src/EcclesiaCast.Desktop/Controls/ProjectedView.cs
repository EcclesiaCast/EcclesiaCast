using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Rendering;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// The full projected picture, in Avalonia: the WPF <c>ProjectedView</c>
/// and, on the real output, what the WPF <c>OutputWindow</c> puts behind it.
/// From the bottom up:
/// 1. the fill of a video background (colour, picture or another video);
/// 2. the background video, which draws its own blurred bars;
/// 3. the picture before, held while the new one fades in over it;
/// 4. the fill of an image background (colour or picture);
/// 5. the background image (a video's poster in the previews);
/// 6. in the previews, the small moving copy of the projector's video;
/// 7. the slide (text, logo, countdown, black, announcement); a video logo
///    is decoded by a hidden player of its own and drawn in the logo layer.
///
/// In WPF the three players live in the output window and this control sits
/// on top of them. Here they live inside, made only when
/// <see cref="IsLiveOutput"/> is set, so the output window of batch 11 is a
/// window with this inside and nothing more.
///
/// Not drawn, because they do not exist on a Mac: the web page panel and its
/// mouse arrow, a captured window and YouTube. Where they would go is marked
/// below; phase 5 brings them to the Avalonia side on Windows, where
/// <c>IPlatformInfo.IsAvailable</c> says they exist.
/// </summary>
public sealed class ProjectedView : UserControl
{
    public static readonly StyledProperty<MediaItem?> BackgroundMediaProperty =
        AvaloniaProperty.Register<ProjectedView, MediaItem?>(nameof(BackgroundMedia));

    /// <summary>
    /// Another library item painted behind the background, for the churches
    /// whose screen is not the shape of the projector's picture. Null leaves
    /// the flat fill colour.
    /// </summary>
    public static readonly StyledProperty<MediaItem?> FillMediaProperty =
        AvaloniaProperty.Register<ProjectedView, MediaItem?>(nameof(FillMedia));

    public static readonly StyledProperty<SlideContent?> SlideProperty =
        AvaloniaProperty.Register<ProjectedView, SlideContent?>(nameof(Slide));

    public static readonly StyledProperty<OutputState> StateProperty =
        AvaloniaProperty.Register<ProjectedView, OutputState>(nameof(State), OutputState.Content);

    public static readonly StyledProperty<Countdown?> CountdownProperty =
        AvaloniaProperty.Register<ProjectedView, Countdown?>(nameof(Countdown));

    public static readonly StyledProperty<string?> OverlayProperty =
        AvaloniaProperty.Register<ProjectedView, string?>(nameof(Overlay));

    public static readonly StyledProperty<string?> OverlayFontFamilyProperty =
        AvaloniaProperty.Register<ProjectedView, string?>(nameof(OverlayFontFamily));

    public static readonly StyledProperty<double> OverlayFontSizeProperty =
        AvaloniaProperty.Register<ProjectedView, double>(nameof(OverlayFontSize), OverlayStyle.DefaultSize);

    public static readonly StyledProperty<HighlightSet?> HighlightProperty =
        AvaloniaProperty.Register<ProjectedView, HighlightSet?>(nameof(Highlight));

    public static readonly StyledProperty<bool> AnimateTransitionsProperty =
        AvaloniaProperty.Register<ProjectedView, bool>(nameof(AnimateTransitions));

    /// <summary>
    /// True on the real output: the players run here, and a video background
    /// shows its moving picture. False in the previews, where the poster (or
    /// the small copy of the projector's frames) stands in.
    /// </summary>
    public static readonly StyledProperty<bool> IsLiveOutputProperty =
        AvaloniaProperty.Register<ProjectedView, bool>(nameof(IsLiveOutput));

    public static readonly StyledProperty<Logo?> LogoProperty =
        AvaloniaProperty.Register<ProjectedView, Logo?>(nameof(Logo));

    /// <summary>
    /// The moving picture of a video logo, for a preview: the output makes its
    /// own. Whoever sets it calls <see cref="RedrawLogoVideo"/> on every frame.
    /// </summary>
    public static readonly StyledProperty<IImage?> LogoVideoFrameProperty =
        AvaloniaProperty.Register<ProjectedView, IImage?>(nameof(LogoVideoFrame));

    /// <summary>Blur of the background layer, 0–100.</summary>
    public static readonly StyledProperty<double> BackgroundBlurProperty =
        AvaloniaProperty.Register<ProjectedView, double>(nameof(BackgroundBlur));

    /// <summary>
    /// The small copy of the picture the projector is drawing, for a preview,
    /// so a video background moves there too. Whoever sets it calls
    /// <see cref="RedrawLiveVideo"/> every time it was written.
    /// </summary>
    public static readonly StyledProperty<IImage?> LiveVideoFrameProperty =
        AvaloniaProperty.Register<ProjectedView, IImage?>(nameof(LiveVideoFrame));

    // ── Capas ────────────────────────────────────────────────────

    private readonly Grid _root;

    // Only on the real output: the fill of a video background and the players.
    private Rectangle? _videoFillColor;
    private Image? _videoFillImage;
    private VlcVideoSurface? _fillVideo;
    private VlcVideoSurface? _video;
    private VlcVideoSurface? _logoVideo;

    private readonly Grid _previousLayer;
    private readonly Rectangle _previousFillColor;
    private readonly Image _previousFillImage;
    private readonly Image _previousImage;

    private readonly Rectangle _fillColorLayer;
    private readonly Image _fillImage;
    private readonly Image _backgroundImage;
    private readonly Image _liveVideo;
    private readonly SlideView _slideView;

    public ProjectedView()
    {
        _previousFillColor = new Rectangle { IsVisible = false };
        _previousFillImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _previousImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _previousLayer = new Grid { IsHitTestVisible = false };
        _previousLayer.Children.Add(_previousFillColor);
        _previousLayer.Children.Add(_previousFillImage);
        _previousLayer.Children.Add(_previousImage);

        _fillColorLayer = new Rectangle { IsVisible = false };
        _fillImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _backgroundImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _liveVideo = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        _slideView = new SlideView();

        // Transparent: whatever is behind this control shows where nothing is.
        _root = new Grid { Background = Brushes.Transparent, ClipToBounds = true };
        _root.Children.Add(_previousLayer);
        _root.Children.Add(_fillColorLayer);
        _root.Children.Add(_fillImage);
        _root.Children.Add(_backgroundImage);
        _root.Children.Add(_liveVideo);
        // Here, between the picture and the slide, WPF draws the operator's
        // mouse arrow over a projected web page (WebPointerLayer), and a web
        // page or a captured window is drawn in _liveVideo. Neither exists on
        // a Mac (Feature.WebPanel, Feature.WindowCapture): phase 5.
        _root.Children.Add(_slideView);
        Content = _root;

        AttachedToVisualTree += (_, _) =>
        {
            PerformanceMode.Changed += OnPerformanceChanged;
            TransitionSettings.Changed += OnPerformanceChanged;
            ApplyPerformance();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            PerformanceMode.Changed -= OnPerformanceChanged;
            TransitionSettings.Changed -= OnPerformanceChanged;
        };
    }

    /// <summary>The slide renderer, so callers can reach its members.</summary>
    public SlideView SlideView => _slideView;

    /// <summary>The background video's player on the real output (null in a preview).</summary>
    public VlcVideoSurface? Video => _video;

    /// <summary>The player of a video logo on the real output (null in a preview).</summary>
    internal VlcVideoSurface? LogoVideo => _logoVideo;

    /// <summary>The player of a video fill on the real output (null in a preview).</summary>
    internal VlcVideoSurface? FillVideo => _fillVideo;

    internal Image BackgroundImage => _backgroundImage;
    internal Image PreviousImage => _previousImage;
    internal Image FillImage => _fillImage;
    internal Rectangle FillColorLayer => _fillColorLayer;
    internal Image LiveVideo => _liveVideo;

    public MediaItem? BackgroundMedia { get => GetValue(BackgroundMediaProperty); set => SetValue(BackgroundMediaProperty, value); }
    public MediaItem? FillMedia { get => GetValue(FillMediaProperty); set => SetValue(FillMediaProperty, value); }
    public SlideContent? Slide { get => GetValue(SlideProperty); set => SetValue(SlideProperty, value); }
    public OutputState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public Countdown? Countdown { get => GetValue(CountdownProperty); set => SetValue(CountdownProperty, value); }
    public string? Overlay { get => GetValue(OverlayProperty); set => SetValue(OverlayProperty, value); }
    public string? OverlayFontFamily { get => GetValue(OverlayFontFamilyProperty); set => SetValue(OverlayFontFamilyProperty, value); }
    public double OverlayFontSize { get => GetValue(OverlayFontSizeProperty); set => SetValue(OverlayFontSizeProperty, value); }
    public HighlightSet? Highlight { get => GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }
    public bool AnimateTransitions { get => GetValue(AnimateTransitionsProperty); set => SetValue(AnimateTransitionsProperty, value); }
    public bool IsLiveOutput { get => GetValue(IsLiveOutputProperty); set => SetValue(IsLiveOutputProperty, value); }
    public Logo? Logo { get => GetValue(LogoProperty); set => SetValue(LogoProperty, value); }
    public IImage? LogoVideoFrame { get => GetValue(LogoVideoFrameProperty); set => SetValue(LogoVideoFrameProperty, value); }
    public double BackgroundBlur { get => GetValue(BackgroundBlurProperty); set => SetValue(BackgroundBlurProperty, value); }
    public IImage? LiveVideoFrame { get => GetValue(LiveVideoFrameProperty); set => SetValue(LiveVideoFrameProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SlideProperty)
            _slideView.Slide = Slide;
        else if (change.Property == StateProperty)
        {
            _slideView.State = State;
            UpdateLogoVideo();
            UpdateLogoCover();
        }
        else if (change.Property == CountdownProperty)
            _slideView.Countdown = Countdown;
        else if (change.Property == OverlayProperty)
            _slideView.Overlay = Overlay;
        else if (change.Property == OverlayFontFamilyProperty)
            _slideView.OverlayFontFamily = OverlayFontFamily;
        else if (change.Property == OverlayFontSizeProperty)
            _slideView.OverlayFontSize = OverlayFontSize;
        else if (change.Property == HighlightProperty)
            _slideView.Highlight = Highlight;
        else if (change.Property == AnimateTransitionsProperty)
            _slideView.AnimateTransitions = AnimateTransitions;
        else if (change.Property == LogoProperty)
        {
            _slideView.Logo = Logo;
            UpdateLogoVideo();
        }
        else if (change.Property == LogoVideoFrameProperty)
        {
            // On the output the logo's own player gives the frame.
            if (!IsLiveOutput)
                _slideView.LogoVideoFrame = LogoVideoFrame;
        }
        else if (change.Property == IsLiveOutputProperty)
        {
            _slideView.IsLiveOutput = IsLiveOutput;
            if (IsLiveOutput)
                EnsurePlayers();
            OnBackgroundChanged();
            UpdateVideo();
            UpdateLogoVideo();
            ApplyPerformance();
        }
        else if (change.Property == BackgroundMediaProperty)
        {
            OnBackgroundChanged();
            UpdateVideo();
        }
        else if (change.Property == FillMediaProperty)
        {
            ApplyFraming();
            UpdateVideoFill();
        }
        else if (change.Property == BackgroundBlurProperty)
            ApplyBlur(BackgroundBlur);
        else if (change.Property == LiveVideoFrameProperty)
            ApplyLiveVideo();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        // The framing is expressed over a 1920×1080 canvas, so it is redone
        // whenever this control changes size.
        if (finalSize != _arrangedSize)
        {
            _arrangedSize = finalSize;
            ApplyFraming();
        }

        return arranged;
    }

    private Size _arrangedSize;

    private double ActualWidth => _arrangedSize.Width;
    private double ActualHeight => _arrangedSize.Height;

    /// <summary>The small copy given in <see cref="LiveVideoFrame"/> was written again.</summary>
    public void RedrawLiveVideo()
    {
        if (_liveVideo.IsVisible)
            _liveVideo.InvalidateVisual();
    }

    /// <summary>The frame given in <see cref="LogoVideoFrame"/> was written again.</summary>
    public void RedrawLogoVideo() => _slideView.RedrawLogoVideo();

    // ── Rendimiento ──────────────────────────────────────────────

    private void OnPerformanceChanged(object? sender, EventArgs e) => ApplyPerformance();

    /// <summary>How long one medium takes to give way to the next; zero in «bajo rendimiento».</summary>
    private static TimeSpan MediaFade =>
        PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Media;

    /// <summary>
    /// «Modo PC de bajo rendimiento»: in a preview the text loses its shadow
    /// and outline and a video shows its poster. On the output the media cut
    /// instead of fading, and the video stops being copied for the operator.
    /// </summary>
    private void ApplyPerformance()
    {
        _slideView.LowDetail = PerformanceMode.IsLow && !IsLiveOutput;

        if (_video is not null && _logoVideo is not null && _fillVideo is not null)
        {
            _video.HoldFade = _logoVideo.HoldFade = _fillVideo.HoldFade = MediaFade;

            var forward = !PerformanceMode.IsLow && !_backgroundIsHeavy;
            if (!forward && _forwardingFrames)
            {
                _video.PreviewFrameChanged -= OnPreviewFrame;
                _video.PreviewFrameWritten -= OnPreviewFrameWritten;
                _forwardingFrames = false;
                VideoFrameChanged?.Invoke(this, null);
            }
            else if (forward && !_forwardingFrames)
            {
                _video.PreviewFrameChanged += OnPreviewFrame;
                _video.PreviewFrameWritten += OnPreviewFrameWritten;
                _forwardingFrames = true;
                _video.ResetPreview();
            }
        }

        ApplyLiveVideo();
    }

    // ── Los reproductores de la salida ───────────────────────────

    /// <summary>Raised when the projected video finishes and shouldn't loop.</summary>
    public event EventHandler? VideoEnded;

    /// <summary>Raised when the small copy of the video for the operator's preview is replaced or cleared.</summary>
    public event EventHandler<IImage?>? VideoFrameChanged;

    /// <summary>Raised every time new pixels were written into that small copy.</summary>
    public event EventHandler? VideoFrameWritten;

    /// <summary>The frames of a video logo, for the operator's Live panel.</summary>
    public event EventHandler<IImage?>? LogoFrameChanged;

    /// <summary>Raised every time a frame of the video logo was written.</summary>
    public event EventHandler? LogoFrameWritten;

    private bool _forwardingFrames;
    private int? _logoVideoId;
    private int _logoVideoVersion;

    private void OnPreviewFrame(object? sender, IImage? frame) => VideoFrameChanged?.Invoke(this, frame);

    private void OnPreviewFrameWritten(object? sender, EventArgs e) => VideoFrameWritten?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// The three players of the output, each with its own surface: one VLC
    /// player per surface is the arrangement that keeps them from tripping
    /// over one another. Made once, the first time this is the real output.
    /// </summary>
    private void EnsurePlayers()
    {
        if (_video is not null)
            return;

        _videoFillColor = new Rectangle { IsVisible = false, IsHitTestVisible = false };
        _videoFillImage = new Image { Stretch = Stretch.UniformToFill, IsVisible = false, IsHitTestVisible = false };
        _fillVideo = new VlcVideoSurface { IsVisible = false };
        _video = new VlcVideoSurface { DrawsOwnBackdrop = true };

        // The logo's video is never seen here: it only decodes, and its frames
        // are drawn in the logo layer, above any background.
        _logoVideo = new VlcVideoSurface { IsVisible = false };

        // Here, above the background video, WPF puts YouTube (a web view):
        // there is no YouTube on a Mac (Feature.YouTubeLive). Phase 5.
        _root.Children.Insert(0, _videoFillColor);
        _root.Children.Insert(1, _videoFillImage);
        _root.Children.Insert(2, _fillVideo);
        _root.Children.Insert(3, _video);
        _root.Children.Add(_logoVideo);

        _video.Ended += (_, _) => VideoEnded?.Invoke(this, EventArgs.Empty);

        // Changing media never goes through black: whatever was on screen
        // stays until the new picture is up, then fades away.
        // • Image to video: the held image goes when the video draws.
        _video.FirstFrame += (_, _) => ReleaseHeldImage(MediaFade);
        // • Video to image: the video's last picture goes when the image has
        //   finished fading in over it.
        BackgroundImageSettled += (_, _) => _video.ReleaseHold(TimeSpan.Zero);

        _logoVideo.FrameSourceChanged += (_, frame) =>
        {
            if (_logoVideoId is not null)
            {
                _slideView.LogoVideoFrame = frame;
                LogoFrameChanged?.Invoke(this, frame);
            }
        };
        _logoVideo.FrameWritten += (_, _) =>
        {
            if (_logoVideoId is not null)
            {
                _slideView.RedrawLogoVideo();
                LogoFrameWritten?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    /// <summary>The background is a heavy video (4K, 50+ frames): the operator gets its poster.</summary>
    private bool _backgroundIsHeavy;

    private void UpdateVideo()
    {
        if (_video is null || !IsLiveOutput)
            return;

        var background = BackgroundMedia;
        var heavy = background is not null && LightVideoCache.IsHeavy(background);
        if (heavy != _backgroundIsHeavy)
        {
            _backgroundIsHeavy = heavy;
            ApplyPerformance();
        }

        // A YouTube background would go to its own player here (see above).
        if (background is { Type: MediaType.Image })
        {
            // The image fades in over the video's last picture; when it is
            // fully up, BackgroundImageSettled lets that picture go.
            _video.Stop(holdLastFrame: true);
        }
        else if (background is null)
        {
            // Nothing replaces it: the last picture fades out instead of cutting.
            _video.Stop(holdLastFrame: true);
            _video.ReleaseHold(MediaFade);
        }
        else
        {
            _video.Show(background);
        }

        UpdateVideoFill();
        _video.SetBlur(BackgroundBlur);

        // A video put on while the logo is up stays paused under it.
        UpdateLogoCover();
    }

    /// <summary>
    /// Paints whatever a video background does not cover: a colour, or
    /// another item of the library (often a blurred copy of the same video).
    /// An image background paints its own fill, in the layers above.
    /// </summary>
    private void UpdateVideoFill()
    {
        if (_videoFillColor is null || _videoFillImage is null || _fillVideo is null)
            return;

        var background = BackgroundMedia;
        var needsFill = background?.Type is not (MediaType.Image or MediaType.Web or MediaType.Window)
                        && MediaFraming.NeedsFill(background);
        var fill = needsFill ? FillMedia : null;

        _videoFillColor.Fill = MediaFraming.FillBrush(background);
        _videoFillColor.IsVisible = needsFill;

        var fillImage = fill?.Type == MediaType.Image ? fill.Path : null;
        _videoFillImage.Source = fillImage is null ? null : Pictures.Load(fillImage);
        _videoFillImage.IsVisible = _videoFillImage.Source is not null;

        var fillVideo = fill?.Type == MediaType.Video ? fill : null;
        _fillVideo.IsVisible = fillVideo is not null;
        _fillVideo.Show(fillVideo is null ? null : AsFillVideo(fillVideo));
    }

    /// <summary>
    /// The fill video always loops, is always silent and always covers the
    /// screen: it is scenery behind the real background.
    /// </summary>
    private static MediaItem AsFillVideo(MediaItem fill)
    {
        var copy = fill.Clone();
        copy.EndBehavior = VideoEndBehavior.Loop;
        copy.Muted = true;
        copy.Scaling = MediaScaling.Fill;
        copy.Zoom = 1;
        copy.OffsetX = copy.OffsetY = 0;
        copy.FrameWidth = copy.FrameHeight = null;
        return copy;
    }

    /// <summary>
    /// With the logo or black up nothing behind them needs to move: the
    /// background video and its fill are paused, and pick up where they were.
    /// The logo's own video keeps playing.
    /// </summary>
    private void UpdateLogoCover()
    {
        var covered = State is OutputState.Logo or OutputState.Black;
        _video?.SetCovered(covered);
        _fillVideo?.SetCovered(covered);
    }

    /// <summary>
    /// A video logo is decoded by its own player, never on screen: its frames
    /// are drawn in the logo layer of the slide, like an image logo, so it
    /// fades, frames and fills with everything an image logo has.
    /// </summary>
    private void UpdateLogoVideo()
    {
        if (_logoVideo is null || !IsLiveOutput)
            return;

        var logo = Logo;
        var showing = State == OutputState.Logo
                      && logo is { Kind: LogoKind.Video }
                      && !string.IsNullOrWhiteSpace(logo.Path);

        var version = ++_logoVideoVersion;
        if (showing)
        {
            // Another video: its poster shows until its first frame arrives.
            if (_logoVideoId != logo!.Id)
            {
                _logoVideoId = logo.Id;
                _slideView.LogoVideoFrame = null;
                LogoFrameChanged?.Invoke(this, null);
            }

            _logoVideo.Show(logo.AsFramedMedia());
            return;
        }

        void Remove()
        {
            // Asked for again in the meantime: leave it.
            if (version != _logoVideoVersion)
                return;

            _logoVideo.Show(null);
            _logoVideoId = null;
            _slideView.LogoVideoFrame = null;
            LogoFrameChanged?.Invoke(this, null);
        }

        // The video keeps playing while the logo layer fades away.
        var fade = PerformanceMode.IsLow ? TimeSpan.Zero : TransitionSettings.Current.Logo;
        if (fade > TimeSpan.Zero && _logoVideoId is not null)
            DispatcherTimer.RunOnce(Remove, fade);
        else
            Remove();
    }

    // ── Transporte, para la barra de reproducción del operador ───

    /// <summary>Where the projected video is.</summary>
    public PlaybackState PlaybackState => _video?.State ?? PlaybackState.None;

    public void TogglePlayPause() => _video?.TogglePlayPause();

    public void SeekTo(TimeSpan position) => _video?.SeekTo(position);

    public void Skip(TimeSpan delta) => _video?.Skip(delta);

    /// <summary>Switching the output off must stop the sound and the picture too.</summary>
    public void PauseForHiddenOutput()
    {
        _video?.PauseForHiddenOutput();
        _logoVideo?.PauseForHiddenOutput();
        _fillVideo?.PauseForHiddenOutput();
        // The Live panel shows the logo's poster, still, while the output is off.
        LogoFrameChanged?.Invoke(this, null);
    }

    public void ResumeAfterHiddenOutput()
    {
        _video?.ResumeAfterHiddenOutput();
        _logoVideo?.ResumeAfterHiddenOutput();
        _fillVideo?.ResumeAfterHiddenOutput();
        if (_logoVideoId is not null)
            LogoFrameChanged?.Invoke(this, _slideView.LogoVideoFrame);
    }

    /// <summary>Stops every player (the output is closing).</summary>
    public void StopVideos()
    {
        _video?.Stop();
        _logoVideo?.Stop();
        _fillVideo?.Stop();
    }

    // ── Desenfoque ───────────────────────────────────────────────

    private double _blurRadius;

    private void ApplyBlur(double amount)
    {
        _video?.SetBlur(amount);

        var radius = Math.Clamp(amount, 0, 100) * 0.6;
        _blurRadius = radius;
        MediaFraming.SetBlur(_backgroundImage, radius);
        if (_previousImage.IsVisible)
            MediaFraming.SetBlur(_previousImage, radius);
    }

    // ── La imagen de fondo ───────────────────────────────────────

    private string? _lastImagePath;
    private int _lastDecodeWidth = ImageDecodeSize.Baseline;
    private long _lastImageStamp;

    /// <summary>Bumped on every change of background: the answer of an older read is ignored.</summary>
    private int _loadVersion;

    /// <summary>A new picture is being read on another thread; the layers still show the old one.</summary>
    private bool _loadPending;

    /// <summary>The last picture that could not be read: it is not waited for again.</summary>
    private string? _loadFailedPath;

    /// <summary>A heavy video background: its poster, never a moving copy, in a preview.</summary>
    private bool _previewIsHeavy;

    private void OnBackgroundChanged()
    {
        var media = BackgroundMedia;
        _previewIsHeavy = !IsLiveOutput && media is not null && LightVideoCache.IsHeavy(media);

        // The slide sits on top of media: the theme's colour must not cover it.
        _slideView.IsOverMedia = media is not null;

        // Images draw here. A video shows its poster in a preview; on the
        // output its moving picture is in the layers below.
        var path = media?.Type == MediaType.Image
            ? media.Path
            : IsLiveOutput ? null : media?.ThumbnailPath;

        var decodeWidth = media?.Type == MediaType.Image ? Pictures.DecodeWidthFor(media) : ImageDecodeSize.Baseline;
        var stamp = Pictures.StampOf(path);

        // A new picture is read on another thread: the old one stays on
        // screen until it is ready, then this runs again and finds it read.
        var loadVersion = ++_loadVersion;
        _loadPending = false;
        if (media?.Type == MediaType.Image && stamp != 0 && path is not null
            && path != _lastImagePath && path != _loadFailedPath
            && Pictures.Cached(path, decodeWidth) is null)
        {
            _loadPending = true;
            var pendingPath = path;
            var pendingWidth = decodeWidth;
            _ = Task.Run(() => Pictures.Load(pendingPath, pendingWidth) is not null)
                .ContinueWith(done => Dispatcher.UIThread.Post(() =>
                {
                    if (loadVersion != _loadVersion)
                        return;
                    if (!done.Result)
                        _loadFailedPath = pendingPath;
                    OnBackgroundChanged();
                }), TaskScheduler.Default);
            return;
        }

        // The fill on screen now is kept for the old picture, which may stay
        // underneath while the new one fades in.
        NoteOutgoingFill();
        ApplyFraming();

        if (path == _lastImagePath && decodeWidth == _lastDecodeWidth && stamp == _lastImageStamp)
            return;

        // The fade is for a new picture; a re-read of the same one must not blink.
        var newPicture = path != _lastImagePath;
        _lastImagePath = path;
        _lastDecodeWidth = decodeWidth;
        _lastImageStamp = stamp;

        if (stamp != 0 && Pictures.Load(path, decodeWidth) is { } bitmap)
        {
            var previousSource = _backgroundImage.IsVisible ? _backgroundImage.Source : null;
            var previousMedia = _shownMedia;

            _backgroundImage.Source = bitmap;
            _backgroundImage.IsVisible = true;
            MediaFraming.Apply(_backgroundImage, media, ActualWidth, ActualHeight);
            _shownMedia = media;

            var mediaFade = TransitionSettings.Current.Media;
            if (AnimateTransitions && newPicture && mediaFade > TimeSpan.Zero)
            {
                // The new picture rises over the old one, which stays put until
                // it is fully covered.
                HoldPreviousImage(previousSource, previousMedia);
                _heldImageTimer?.Stop();
                FadeInBackground(++_fadeVersion, mediaFade);
            }
            else if (newPicture)
            {
                _fadeVersion++;
                Fades.Stop(_fillImage, OpacityProperty);
                Fades.Stop(_fillColorLayer, OpacityProperty);
                _fillImage.Opacity = _fillColorLayer.Opacity = 1;
                Fades.Stop(_backgroundImage, OpacityProperty);
                _backgroundImage.Opacity = 1;
                ClearPreviousImage();
                // After the picture has been drawn, not before.
                Dispatcher.UIThread.Post(() => BackgroundImageSettled?.Invoke(this, EventArgs.Empty),
                    DispatcherPriority.Background);
            }
            return;
        }

        // On the output a video starts a moment after it is chosen: until its
        // first picture arrives the old image stays (ReleaseHeldImage).
        var videoFollows = IsLiveOutput && media?.Type == MediaType.Video;
        if (videoFollows && _backgroundImage.IsVisible && _backgroundImage.Source is { } held)
        {
            HoldPreviousImage(held, _shownMedia);
            StartHeldImageTimer();
        }
        else if (!videoFollows)
        {
            _fadeVersion++;
            ClearPreviousImage();
        }

        _shownMedia = null;
        _backgroundImage.Source = null;
        _backgroundImage.IsVisible = false;
        MediaFraming.Apply(_backgroundImage, media, ActualWidth, ActualHeight);
    }

    private async void FadeInBackground(int version, TimeSpan duration)
    {
        // What fills the bars of the new picture rises with it.
        _ = Fades.Fade(_fillImage, 0, 1, duration);
        _ = Fades.Fade(_fillColorLayer, 0, 1, duration);
        await Fades.Fade(_backgroundImage, 0, 1, duration, Fades.EaseOut);
        if (version != _fadeVersion)
            return;

        ClearPreviousImage();
        BackgroundImageSettled?.Invoke(this, EventArgs.Empty);
    }

    // ── Cambio de medio sin pasar por negro ──────────────────────

    private MediaItem? _shownMedia;
    private MediaItem? _previousMedia;
    private int _fadeVersion;
    private DispatcherTimer? _heldImageTimer;

    /// <summary>
    /// Raised once the picture of a new image background is fully on screen.
    /// The background video waits for it to let go of its last frame.
    /// </summary>
    public event EventHandler? BackgroundImageSettled;

    /// <summary>
    /// Keeps <paramref name="source"/> drawn underneath, with the framing and
    /// blur it had. An older picture already held is replaced only by one that
    /// now dominates the screen; otherwise it stays, because it is the one the
    /// screen still shows.
    /// </summary>
    private void HoldPreviousImage(IImage? source, MediaItem? media)
    {
        if (_previousImage.IsVisible)
        {
            var incomingDominates = source is not null && _backgroundImage.Opacity >= 0.5;
            if (!incomingDominates)
            {
                Fades.Stop(_previousLayer, OpacityProperty);
                _previousLayer.Opacity = 1;
                return;
            }
        }
        else if (source is null)
        {
            return;
        }

        Fades.Stop(_previousLayer, OpacityProperty);
        _previousLayer.Opacity = 1;
        _previousImage.Source = source;
        _previousMedia = media;
        MediaFraming.Apply(_previousImage, media, ActualWidth, ActualHeight);
        MediaFraming.SetBlur(_previousImage, _blurRadius);
        _previousImage.IsVisible = true;

        // The bars go with the picture they belong to.
        _previousFillImage.Source = _outgoingFillImage;
        _previousFillImage.IsVisible = _outgoingFillImage is not null;
        _previousFillColor.Fill = _outgoingFillColor;
        _previousFillColor.IsVisible = _outgoingFillColor is not null;
    }

    private void ClearPreviousImage()
    {
        _heldImageTimer?.Stop();
        Fades.Stop(_previousLayer, OpacityProperty);
        _previousLayer.Opacity = 1;
        _previousImage.IsVisible = false;
        _previousImage.Source = null;
        _previousFillImage.IsVisible = false;
        _previousFillImage.Source = null;
        _previousFillColor.IsVisible = false;
        _previousMedia = null;
    }

    private IImage? _outgoingFillImage;
    private IBrush? _outgoingFillColor;

    private void NoteOutgoingFill()
    {
        _outgoingFillImage = _fillImage.IsVisible ? _fillImage.Source : null;
        _outgoingFillColor = _fillColorLayer.IsVisible ? _fillColorLayer.Fill : null;
    }

    /// <summary>
    /// Lets go of the image kept for a video that has now drawn its first
    /// picture, fading it out over <paramref name="fade"/> (zero cuts).
    /// </summary>
    public void ReleaseHeldImage(TimeSpan fade)
    {
        _heldImageTimer?.Stop();
        if (!_previousImage.IsVisible)
            return;

        if (fade <= TimeSpan.Zero)
        {
            _fadeVersion++;
            ClearPreviousImage();
            return;
        }

        FadeOutPrevious(++_fadeVersion, fade);
    }

    private async void FadeOutPrevious(int version, TimeSpan fade)
    {
        await Fades.Fade(_previousLayer, 1, 0, fade);
        if (version == _fadeVersion)
            ClearPreviousImage();
    }

    /// <summary>If the video never draws anything, the image is let go anyway.</summary>
    private void StartHeldImageTimer()
    {
        if (_heldImageTimer is null)
        {
            _heldImageTimer = new DispatcherTimer();
            _heldImageTimer.Tick += (_, _) => ReleaseHeldImage(TimeSpan.FromMilliseconds(250));
        }

        _heldImageTimer.Interval = VideoHold.Timeout(TransitionSettings.Current.Media);
        _heldImageTimer.Stop();
        _heldImageTimer.Start();
    }

    // ── Encuadre y relleno ───────────────────────────────────────

    private string? _lastFillImagePath;

    /// <summary>
    /// In a preview: the small copy of the projector's video, framed exactly
    /// as the projector frames it.
    /// </summary>
    private void ApplyLiveVideo()
    {
        // Only a video background has frames to show; anything else must never
        // sit under a leftover frame of the video that played before it.
        var frame = IsLiveOutput || PerformanceMode.IsLow || _previewIsHeavy || BackgroundMedia?.Type != MediaType.Video
            ? null
            : LiveVideoFrame;
        _liveVideo.Source = frame;
        _liveVideo.IsVisible = frame is not null;
        MediaFraming.Apply(_liveVideo, frame is null ? null : BackgroundMedia, ActualWidth, ActualHeight);

        // A poster underneath a live frame is just an older copy of it.
        _backgroundImage.IsVisible = frame is null && _backgroundImage.Source is not null;
    }

    /// <summary>
    /// Places the picture and paints whatever it leaves uncovered. Re-run on
    /// resize too: the framing depends on how big this control is.
    /// </summary>
    private void ApplyFraming()
    {
        // The new picture is still being read: its framing would land on the old one.
        if (_loadPending)
            return;

        var media = BackgroundMedia;
        MediaFraming.Apply(_backgroundImage, media, ActualWidth, ActualHeight);
        if (_previousImage.IsVisible)
            MediaFraming.Apply(_previousImage, _previousMedia, ActualWidth, ActualHeight);

        // The video players frame themselves when they are arranged.

        // On the output the fill here is only for a picture drawn here: a fill
        // over the video would cover the very video it is meant to sit behind.
        var drawsMedia = media is not null && (media.Type is MediaType.Image || !IsLiveOutput);
        var needsFill = drawsMedia && MediaFraming.NeedsFill(media);
        var fill = needsFill ? FillMedia : null;

        // A video used as fill plays only on the output; here its poster.
        var ownFill = needsFill && media is { FillsWithSelf: true } or { FillsWithOwnVideo: true };
        var fillPath = ownFill
            ? media!.Type == MediaType.Image ? media.Path : media.ThumbnailPath
            : fill?.Type == MediaType.Image ? fill.Path : fill?.ThumbnailPath;

        // An image that fills its own bars shows itself, already blurred.
        var selfFill = ownFill && fillPath is not null;
        var fillKey = selfFill ? "blur:" + fillPath : fillPath;

        if (fillKey != _lastFillImagePath)
        {
            _lastFillImagePath = fillKey;
            _fillImage.Source = selfFill
                ? MediaFraming.BlurredFill(Pictures.Load(fillPath!, 480))
                : Pictures.Load(fillPath);
        }

        var showFillImage = needsFill && _fillImage.Source is not null;
        _fillImage.IsVisible = showFillImage;
        _fillColorLayer.IsVisible = needsFill && !showFillImage;
        _fillColorLayer.Fill = MediaFraming.FillBrush(media);

        ApplyLiveVideo();
    }
}
