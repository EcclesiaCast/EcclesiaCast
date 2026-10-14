using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// The full projected picture: a global background layer (image now, video
/// later) with the slide's text and states composited on top. The output
/// window and the operator previews both use it, so what you preview is what
/// projects.
/// </summary>
public partial class ProjectedView : UserControl
{
    public static readonly DependencyProperty BackgroundMediaProperty =
        DependencyProperty.Register(nameof(BackgroundMedia), typeof(MediaItem), typeof(ProjectedView),
            new PropertyMetadata(null, (d, _) => ((ProjectedView)d).OnBackgroundChanged()));

    public static readonly DependencyProperty SlideProperty =
        DependencyProperty.Register(nameof(Slide), typeof(SlideContent), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Slide = (SlideContent?)e.NewValue));

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(OutputState), typeof(ProjectedView),
            new PropertyMetadata(OutputState.Content, (d, e) => ((ProjectedView)d).SlideView.State = (OutputState)e.NewValue));

    /// <summary>The pre-service countdown, handed straight to the slide view.</summary>
    public static readonly DependencyProperty CountdownProperty =
        DependencyProperty.Register(nameof(Countdown), typeof(Countdown), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Countdown = (Countdown?)e.NewValue));

    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Overlay = (string?)e.NewValue));

    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(nameof(Highlight), typeof(HighlightSet), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Highlight = (HighlightSet?)e.NewValue));

    public static readonly DependencyProperty AnimateTransitionsProperty =
        DependencyProperty.Register(nameof(AnimateTransitions), typeof(bool), typeof(ProjectedView),
            new PropertyMetadata(false, (d, e) => ((ProjectedView)d).SlideView.AnimateTransitions = (bool)e.NewValue));

    /// <summary>
    /// True in the output window, where a real video plays behind this
    /// control — so a video background shows nothing here (the video shows
    /// through). False in previews, where the video's poster is shown.
    /// </summary>
    public static readonly DependencyProperty IsLiveOutputProperty =
        DependencyProperty.Register(nameof(IsLiveOutput), typeof(bool), typeof(ProjectedView),
            new PropertyMetadata(false, (d, e) =>
            {
                var view = (ProjectedView)d;
                view.SlideView.IsLiveOutput = (bool)e.NewValue;
                view.OnBackgroundChanged();
                view.ApplyPerformance();
            }));

    /// <summary>The logo shown in the Logo state.</summary>
    public static readonly DependencyProperty LogoProperty =
        DependencyProperty.Register(nameof(Logo), typeof(EcclesiaCast.Core.Logos.Logo), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) =>
                ((ProjectedView)d).SlideView.Logo = (EcclesiaCast.Core.Logos.Logo?)e.NewValue));

    /// <summary>Blur applied to the background image layer, 0–100.</summary>
    public static readonly DependencyProperty BackgroundBlurProperty =
        DependencyProperty.Register(nameof(BackgroundBlur), typeof(double), typeof(ProjectedView),
            new PropertyMetadata(0d, (d, e) => ((ProjectedView)d).ApplyBlur((double)e.NewValue)));

    private string? _lastImagePath;
    private int _lastDecodeWidth = ImageDecodeSize.Baseline;
    private long _lastImageStamp;

    /// <summary>
    /// The background is a heavy video (4K, 50+ frames): in the operator's
    /// panels it stays a still poster, like a YouTube video, instead of a
    /// moving copy of a picture the projector already finds hard to draw.
    /// </summary>
    private bool _backgroundIsHeavy;

    public ProjectedView()
    {
        InitializeComponent();

        // The framing is expressed over a 1920×1080 canvas, so it has to be
        // recomputed whenever this control changes size — the same design
        // shows in a small preview box and on the projector.
        SizeChanged += (_, _) => ApplyFraming();

        // «Modo PC de bajo rendimiento»: listened to only while on screen.
        Loaded += (_, _) =>
        {
            PerformanceMode.Changed += OnPerformanceChanged;
            ApplyPerformance();
        };
        Unloaded += (_, _) =>
        {
            PerformanceMode.Changed -= OnPerformanceChanged;
            HookWebFeed(false);
        };
    }

    private void OnPerformanceChanged(object? sender, EventArgs e) => ApplyPerformance();

    /// <summary>
    /// In the operator's panels the low-performance mode drops the text's
    /// shadow and outline and shows a video's poster instead of its moving
    /// picture. The output window keeps both: that is what the congregation sees.
    /// </summary>
    private void ApplyPerformance()
    {
        SlideRenderer.LowDetail = PerformanceMode.IsLow && !IsLiveOutput;
        ApplyLiveVideo();
    }

    /// <summary>The underlying slide renderer, so callers can reach its members.</summary>
    public SlideView SlideView => SlideRenderer;

    public MediaItem? BackgroundMedia
    {
        get => (MediaItem?)GetValue(BackgroundMediaProperty);
        set => SetValue(BackgroundMediaProperty, value);
    }

    public SlideContent? Slide
    {
        get => (SlideContent?)GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    public OutputState State
    {
        get => (OutputState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public Countdown? Countdown
    {
        get => (Countdown?)GetValue(CountdownProperty);
        set => SetValue(CountdownProperty, value);
    }

    public string? Overlay
    {
        get => (string?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public HighlightSet? Highlight
    {
        get => (HighlightSet?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    public bool AnimateTransitions
    {
        get => (bool)GetValue(AnimateTransitionsProperty);
        set => SetValue(AnimateTransitionsProperty, value);
    }

    public bool IsLiveOutput
    {
        get => (bool)GetValue(IsLiveOutputProperty);
        set => SetValue(IsLiveOutputProperty, value);
    }

    public double BackgroundBlur
    {
        get => (double)GetValue(BackgroundBlurProperty);
        set => SetValue(BackgroundBlurProperty, value);
    }

    public EcclesiaCast.Core.Logos.Logo? Logo
    {
        get => (EcclesiaCast.Core.Logos.Logo?)GetValue(LogoProperty);
        set => SetValue(LogoProperty, value);
    }

    private double _blurRadius;

    private void ApplyBlur(double amount)
    {
        var radius = Math.Clamp(amount, 0, 100) * 0.6;
        _blurRadius = radius;
        // Through the framing, which knows how much it scales the picture.
        MediaFraming.SetBlur(BackgroundImage, radius);
        if (PreviousImage.Visibility == Visibility.Visible)
            MediaFraming.SetBlur(PreviousImage, radius);
        if (BackgroundMedia?.Type == MediaType.Web)
            MediaFraming.SetBlur(LiveVideo, radius);
    }

    // ── Una página web de fondo ──────────────────────────────────

    private bool _webHooked;
    private System.Windows.Threading.DispatcherTimer? _webSnapshotTimer;
    private ImageSource? _webSnapshot;

    /// <summary>
    /// The page of the web panel as a picture. The output window always gets it
    /// moving. The operator's panels in «bajo rendimiento» get a still copy
    /// that is renewed every two seconds: three views redrawing a whole page on
    /// every frame is what a slow computer cannot afford.
    /// </summary>
    private ImageSource? WebFrame()
    {
        if (IsLiveOutput || !PerformanceMode.IsLow)
            return WebLiveFeed.Source;

        _webSnapshot ??= WebLiveFeed.Snapshot();
        return _webSnapshot;
    }

    /// <summary>
    /// Listens to the feed only while a web page is the background of a view
    /// that is on screen: the feed's events are static, and a view that is gone
    /// must not stay hooked to them.
    /// </summary>
    private void HookWebFeed(bool wanted)
    {
        wanted &= IsLoaded;
        if (wanted == _webHooked)
        {
            UpdateSnapshotTimer(wanted);
            return;
        }

        _webHooked = wanted;
        if (wanted)
        {
            WebLiveFeed.SourceChanged += OnWebFeedChanged;
            WebLiveFeed.PointerChanged += OnWebPointerChanged;
        }
        else
        {
            WebLiveFeed.SourceChanged -= OnWebFeedChanged;
            WebLiveFeed.PointerChanged -= OnWebPointerChanged;
            _webSnapshot = null;
        }

        UpdateSnapshotTimer(wanted);
    }

    private void UpdateSnapshotTimer(bool wanted)
    {
        var needed = wanted && !IsLiveOutput && PerformanceMode.IsLow;
        if (needed && _webSnapshotTimer is null)
        {
            _webSnapshotTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _webSnapshotTimer.Tick += (_, _) =>
            {
                _webSnapshot = WebLiveFeed.Snapshot();
                LiveVideo.Source = _webSnapshot;
                LiveVideo.Visibility = _webSnapshot is null ? Visibility.Collapsed : Visibility.Visible;
            };
            _webSnapshotTimer.Start();
        }
        else if (!needed && _webSnapshotTimer is not null)
        {
            _webSnapshotTimer.Stop();
            _webSnapshotTimer = null;
            _webSnapshot = null;
        }
    }

    private void OnWebFeedChanged(object? sender, EventArgs e)
    {
        _webSnapshot = null;
        ApplyLiveVideo();
    }

    private void OnWebPointerChanged(object? sender, EventArgs e) => ApplyPointer();

    /// <summary>
    /// Draws the operator's mouse arrow on the page, where it is pointing: the
    /// page is fitted whole in this view, so the fraction of the page the feed
    /// reports maps straight onto the rectangle it takes here.
    /// </summary>
    private void ApplyPointer()
    {
        if (BackgroundMedia?.Type != MediaType.Web
            || WebLiveFeed.Pointer is not { } at
            || LiveVideo.Source is not { Width: > 0, Height: > 0 } picture
            || ActualWidth <= 0 || ActualHeight <= 0)
        {
            WebPointerLayer.Visibility = Visibility.Collapsed;
            return;
        }

        var scale = Math.Min(ActualWidth / picture.Width, ActualHeight / picture.Height);
        var width = picture.Width * scale;
        var height = picture.Height * scale;
        var left = (ActualWidth - width) / 2;
        var top = (ActualHeight - height) / 2;

        // The arrow is 19.5 units tall; on the output it is about 4.5 % of the screen's height.
        var size = Math.Max(0.7, ActualHeight * 0.045 / 19.5);
        WebPointer.RenderTransform = new ScaleTransform(size, size);
        Canvas.SetLeft(WebPointer, left + at.X * width);
        Canvas.SetTop(WebPointer, top + at.Y * height);
        WebPointerLayer.Visibility = Visibility.Visible;
    }

    private void OnBackgroundChanged()
    {
        var media = BackgroundMedia;
        _backgroundIsHeavy = !IsLiveOutput && media is not null && LightVideoCache.IsHeavy(media);

        // Let the slide layer know it is sitting on top of media, so the
        // theme's background colour doesn't paint over it.
        SlideRenderer.IsOverMedia = media is not null;

        // Images render directly. Videos show their poster in previews, but
        // in the live output the poster is hidden so the moving video (behind
        // this control) shows through.
        var path = media?.Type == MediaType.Image
            ? media.Path
            : IsLiveOutput ? null : media?.ThumbnailPath; // póster de video/YouTube en los previews

        ApplyFraming();

        // Zoomed in, a picture has to be read wider than the screen or it goes
        // soft. Without zoom this stays the 1920 it always was, and the file is
        // not even opened to find out.
        var decodeWidth = media?.Type == MediaType.Image ? DecodeWidthFor(media, path) : ImageDecodeSize.Baseline;

        // A page drawn again keeps its file name but not its contents.
        var stamp = path is not null && !path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && File.Exists(path)
            ? File.GetLastWriteTimeUtc(path).Ticks
            : 0;

        if (path == _lastImagePath && decodeWidth == _lastDecodeWidth && stamp == _lastImageStamp)
            return;

        // The fade is for a new picture; a re-read of the same one (the zoom
        // was edited while it is on screen) must not blink.
        var newPicture = path != _lastImagePath;
        var changedOnDisk = !newPicture && stamp != _lastImageStamp;
        _lastImagePath = path;
        _lastDecodeWidth = decodeWidth;
        _lastImageStamp = stamp;

        // YouTube posters are remote URLs; local media are files on disk.
        var isRemote = path?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true;

        if (!string.IsNullOrWhiteSpace(path) && (isRemote || File.Exists(path)))
        {
            try
            {
                // What is on screen now, to stay underneath while the new picture fades in.
                var previousSource = BackgroundImage.Visibility == Visibility.Visible ? BackgroundImage.Source : null;
                var previousMedia = _shownMedia;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = decodeWidth;
                // WPF remembers pictures by file name: a file rewritten under
                // the same name would come back as the old one.
                if (changedOnDisk)
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                // A remote poster (YouTube) is still downloading here, and
                // freezing it would throw and drop us into the black fallback.
                // WPF fills the Image in once the download finishes.
                if (bitmap.CanFreeze)
                    bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                // «Rellenar» depends on the shape of the picture just loaded.
                MediaFraming.Apply(BackgroundImage, BackgroundMedia, ActualWidth, ActualHeight);
                _shownMedia = BackgroundMedia;
                var mediaFade = TransitionSettings.Current.Media;
                if (AnimateTransitions && newPicture && mediaFade > TimeSpan.Zero)
                {
                    // The new picture rises over the old one, which stays put
                    // until it is fully covered: fading up from nothing made
                    // the old one vanish at once and the change flash black.
                    HoldPreviousImage(previousSource, previousMedia);
                    var version = ++_fadeVersion;
                    var fade = new DoubleAnimation(0, 1, mediaFade) { EasingFunction = new QuadraticEase() };
                    fade.Completed += (_, _) =>
                    {
                        if (version != _fadeVersion)
                            return;
                        ClearPreviousImage();
                        BackgroundImageSettled?.Invoke(this, EventArgs.Empty);
                    };
                    BackgroundImage.BeginAnimation(OpacityProperty, fade);
                }
                else if (newPicture)
                {
                    _fadeVersion++;
                    // A fade of zero seconds is a cut: no animation may linger.
                    BackgroundImage.BeginAnimation(OpacityProperty, null);
                    ClearPreviousImage();
                    // After the picture has been drawn, not before.
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                        () => BackgroundImageSettled?.Invoke(this, EventArgs.Empty));
                }
                return;
            }
            catch
            {
                // Ilegible: se cae al fondo negro.
            }
        }

        // In the output window a video starts a moment after it is chosen
        // (the decoder opens the file): until its first picture arrives the
        // old image stays, and OutputWindow lets it go (ReleaseHeldImage).
        var videoFollows = IsLiveOutput && media?.Type == MediaType.Video;
        if (videoFollows && BackgroundImage.Visibility == Visibility.Visible && BackgroundImage.Source is { } held)
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
        BackgroundImage.Source = null;
        BackgroundImage.Visibility = Visibility.Collapsed;
        MediaFraming.Apply(BackgroundImage, BackgroundMedia, ActualWidth, ActualHeight);
    }

    // ── Cambio de medio sin pasar por negro ──────────────────────

    /// <summary>The item whose framing the picture in <c>BackgroundImage</c> was drawn with.</summary>
    private MediaItem? _shownMedia;

    private MediaItem? _previousMedia;
    private int _fadeVersion;
    private System.Windows.Threading.DispatcherTimer? _heldImageTimer;

    /// <summary>
    /// Raised once the picture of a new image background is fully on screen
    /// (its fade ended, or it was drawn at once). The output window waits for
    /// it to let go of the last frame of the video that was there before.
    /// </summary>
    public event EventHandler? BackgroundImageSettled;

    /// <summary>
    /// Keeps <paramref name="source"/> drawn underneath the background layer,
    /// with the framing and blur it had. If an older picture is already held
    /// it stays: it is the one the screen still shows.
    /// </summary>
    private void HoldPreviousImage(ImageSource? source, MediaItem? media)
    {
        if (PreviousImage.Visibility == Visibility.Visible)
        {
            // Already holding one (maybe halfway through fading away): it is
            // what the screen still shows, so it goes back to full strength.
            PreviousImage.BeginAnimation(OpacityProperty, null);
            PreviousImage.Opacity = 1;
            return;
        }

        if (source is null)
            return;

        PreviousImage.BeginAnimation(OpacityProperty, null);
        PreviousImage.Opacity = 1;
        PreviousImage.Source = source;
        _previousMedia = media;
        MediaFraming.Apply(PreviousImage, media, ActualWidth, ActualHeight);
        MediaFraming.SetBlur(PreviousImage, _blurRadius);
        PreviousImage.Visibility = Visibility.Visible;
    }

    private void ClearPreviousImage()
    {
        _heldImageTimer?.Stop();
        PreviousImage.BeginAnimation(OpacityProperty, null);
        PreviousImage.Opacity = 1;
        PreviousImage.Visibility = Visibility.Collapsed;
        PreviousImage.Source = null;
        _previousMedia = null;
    }

    /// <summary>
    /// Lets go of the image kept for a video that has now drawn its first
    /// picture, fading it out over <paramref name="fade"/> (zero cuts).
    /// </summary>
    public void ReleaseHeldImage(TimeSpan fade)
    {
        _heldImageTimer?.Stop();
        if (PreviousImage.Visibility != Visibility.Visible)
            return;

        if (fade <= TimeSpan.Zero)
        {
            _fadeVersion++;
            ClearPreviousImage();
            return;
        }

        var version = ++_fadeVersion;
        var animation = new DoubleAnimation(0, fade);
        animation.Completed += (_, _) =>
        {
            if (version == _fadeVersion)
                ClearPreviousImage();
        };
        PreviousImage.BeginAnimation(OpacityProperty, animation);
    }

    /// <summary>If the video never draws anything, the image is let go after two seconds.</summary>
    private void StartHeldImageTimer()
    {
        if (_heldImageTimer is null)
        {
            _heldImageTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _heldImageTimer.Tick += (_, _) => ReleaseHeldImage(TimeSpan.FromMilliseconds(250));
        }

        _heldImageTimer.Stop();
        _heldImageTimer.Start();
    }

    /// <summary>
    /// The width to read this image at: the screen's, unless the media is
    /// zoomed in, in which case it also depends on the file's own size.
    /// </summary>
    private static int DecodeWidthFor(MediaItem media, string? path)
    {
        if (!ImageDecodeSize.NeedsFileSize(media.Zoom) || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return ImageDecodeSize.Baseline;

        try
        {
            // Only the header is read: no pixels are decoded to learn the size.
            var frame = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None)
                .Frames[0];
            return ImageDecodeSize.For(
                media.Scaling, media.Zoom, media.FrameWidth, media.FrameHeight, frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return ImageDecodeSize.Baseline;
        }
    }

    // ── Encuadre y relleno ───────────────────────────────────────

    /// <summary>
    /// Another library item painted behind the background, for the churches
    /// whose screen is not the shape of the projector's picture. Null leaves
    /// the flat fill colour.
    /// </summary>
    public static readonly DependencyProperty FillMediaProperty =
        DependencyProperty.Register(nameof(FillMedia), typeof(MediaItem), typeof(ProjectedView),
            new PropertyMetadata(null, (d, _) => ((ProjectedView)d).ApplyFraming()));

    public MediaItem? FillMedia
    {
        get => (MediaItem?)GetValue(FillMediaProperty);
        set => SetValue(FillMediaProperty, value);
    }

    private string? _lastFillImagePath;

    /// <summary>
    /// The picture the projector is drawing right now. Set on the operator's
    /// previews so a video background moves there too; the output window
    /// leaves it null, since it draws the real thing behind this control.
    /// </summary>
    public static readonly DependencyProperty LiveVideoFrameProperty =
        DependencyProperty.Register(nameof(LiveVideoFrame), typeof(ImageSource), typeof(ProjectedView),
            new PropertyMetadata(null, (d, _) => ((ProjectedView)d).ApplyLiveVideo()));

    public ImageSource? LiveVideoFrame
    {
        get => (ImageSource?)GetValue(LiveVideoFrameProperty);
        set => SetValue(LiveVideoFrameProperty, value);
    }

    private void ApplyLiveVideo()
    {
        var isWeb = BackgroundMedia?.Type == MediaType.Web;
        HookWebFeed(isWeb);

        // Only a video background has frames to show. Anything else (an
        // image, say the offering slide) must never sit under a leftover
        // frame of the video that played before it. A web page is the other
        // case: its frames come from the web panel's browser, in every view.
        var frame = isWeb
            ? WebFrame()
            : IsLiveOutput || PerformanceMode.IsLow || _backgroundIsHeavy || BackgroundMedia?.Type != MediaType.Video
                ? null
                : LiveVideoFrame;
        LiveVideo.Source = frame;
        LiveVideo.Visibility = frame is null ? Visibility.Collapsed : Visibility.Visible;

        // The framing (zoom, size, shift) applies to the preview exactly as it
        // does on the projector, so what the operator sees is what goes out.
        MediaFraming.Apply(LiveVideo, frame is null ? null : BackgroundMedia, ActualWidth, ActualHeight);
        MediaFraming.SetBlur(LiveVideo, isWeb ? _blurRadius : 0);
        ApplyPointer();

        // A poster underneath a live frame is just an older copy of it; once
        // the frames stop (output closed, video gone) the poster comes back.
        BackgroundImage.Visibility = frame is null && BackgroundImage.Source is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Places the picture and paints whatever it leaves uncovered. Re-run on
    /// resize too: the framing is expressed over the 1920×1080 canvas, so it
    /// depends on how big this control currently is.
    /// </summary>
    private void ApplyFraming()
    {
        var media = BackgroundMedia;
        MediaFraming.Apply(BackgroundImage, media, ActualWidth, ActualHeight);
        if (PreviousImage.Visibility == Visibility.Visible)
            MediaFraming.Apply(PreviousImage, _previousMedia, ActualWidth, ActualHeight);

        // This layer sits ON TOP of the output window's video surface, so it
        // may only paint the fill when it is the one drawing the picture —
        // otherwise the fill covers the very video it is meant to sit behind.
        var drawsMedia = media is not null && (media.Type is MediaType.Image or MediaType.Web || !IsLiveOutput);
        var needsFill = drawsMedia && MediaFraming.NeedsFill(media);
        var fill = needsFill ? FillMedia : null;

        // A video used as fill can only play in the output window, which owns
        // the VLC surfaces; here its poster stands in, exactly like the
        // background video's poster does in the previews.
        var fillPath = fill?.Type == MediaType.Image ? fill.Path : fill?.ThumbnailPath;

        if (fillPath != _lastFillImagePath)
        {
            _lastFillImagePath = fillPath;
            FillImage.Source = LoadBitmap(fillPath);
        }

        var showFillImage = needsFill && FillImage.Source is not null;
        FillImage.Visibility = showFillImage ? Visibility.Visible : Visibility.Collapsed;

        FillColorLayer.Visibility = needsFill && !showFillImage ? Visibility.Visible : Visibility.Collapsed;
        FillColorLayer.Fill = MediaFraming.FillBrush(media);

        ApplyColourAdjustments(media);
        ApplyLiveVideo();
    }

    /// <summary>
    /// Darkening and tinting ride on this layer, which sits above both the
    /// image drawn here and the video playing behind the control — so one
    /// implementation covers photos, loops and YouTube alike.
    /// </summary>
    private void ApplyColourAdjustments(MediaItem? media) =>
        MediaFraming.ApplyColour(BrightnessLayer, TintLayer, media);

    private static BitmapImage? LoadBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 1920;
            bitmap.EndInit();
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
