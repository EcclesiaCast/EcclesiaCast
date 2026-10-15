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
            new PropertyMetadata(OutputState.Content, (d, e) =>
            {
                var view = (ProjectedView)d;
                view.SlideView.State = (OutputState)e.NewValue;
                // With the logo up a live page or window is not redrawn (see ApplyLiveVideo).
                view.ApplyLiveVideo();
            }));

    /// <summary>The pre-service countdown, handed straight to the slide view.</summary>
    public static readonly DependencyProperty CountdownProperty =
        DependencyProperty.Register(nameof(Countdown), typeof(Countdown), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Countdown = (Countdown?)e.NewValue));

    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Overlay = (string?)e.NewValue));

    /// <summary>Typeface and size of the lower-third announcement, handed to the slide view.</summary>
    public static readonly DependencyProperty OverlayFontFamilyProperty =
        DependencyProperty.Register(nameof(OverlayFontFamily), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.OverlayFontFamily = (string?)e.NewValue));

    public static readonly DependencyProperty OverlayFontSizeProperty =
        DependencyProperty.Register(nameof(OverlayFontSize), typeof(double), typeof(ProjectedView),
            new PropertyMetadata(OverlayStyle.DefaultSize, (d, e) => ((ProjectedView)d).SlideView.OverlayFontSize = (double)e.NewValue));

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

    /// <summary>Bumped on every change of background: the answer of an older read must be ignored.</summary>
    private int _loadVersion;

    /// <summary>A new picture is being read on another thread; the layers still show the old one.</summary>
    private bool _loadPending;

    /// <summary>The last picture that could not be read: it is not waited for again.</summary>
    private string? _loadFailedPath;

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
            HookFeed(null);
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

    public string? OverlayFontFamily
    {
        get => (string?)GetValue(OverlayFontFamilyProperty);
        set => SetValue(OverlayFontFamilyProperty, value);
    }

    public double OverlayFontSize
    {
        get => (double)GetValue(OverlayFontSizeProperty);
        set => SetValue(OverlayFontSizeProperty, value);
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
        if (BackgroundMedia?.Type is MediaType.Web or MediaType.Window)
            MediaFraming.SetBlur(LiveVideo, radius);
    }

    // ── Una página web o una ventana de fondo ────────────────────
    // Both are live pictures made elsewhere (the web panel's browser, the
    // window capture) and drawn here the same way, in every view.

    /// <summary>Which live picture this view is listening to, if any.</summary>
    private MediaType? _hookedFeed;
    private System.Windows.Threading.DispatcherTimer? _webSnapshotTimer;
    private ImageSource? _webSnapshot;

    /// <summary>
    /// The live picture of the background (the web page, or the captured
    /// window). The output window always gets it moving. The operator's panels
    /// in «bajo rendimiento» get a still copy that is renewed every two
    /// seconds: three views redrawing a whole page on every frame is what a
    /// slow computer cannot afford.
    /// </summary>
    private ImageSource? FeedFrame()
    {
        var window = BackgroundMedia?.Type == MediaType.Window;
        if (IsLiveOutput || !PerformanceMode.IsLow)
            return window ? WindowCaptureFeed.Source : WebLiveFeed.Source;

        _webSnapshot ??= FeedSnapshot();
        return _webSnapshot;
    }

    private BitmapSource? FeedSnapshot() =>
        BackgroundMedia?.Type == MediaType.Window ? WindowCaptureFeed.Snapshot() : WebLiveFeed.Snapshot();

    /// <summary>
    /// Listens to the feed only while a web page or a window is the background
    /// of a view that is on screen: the feed's events are static, and a view
    /// that is gone must not stay hooked to them.
    /// </summary>
    private void HookFeed(MediaType? feed)
    {
        feed = IsLoaded ? feed : null;
        if (feed == _hookedFeed)
        {
            UpdateSnapshotTimer(feed is not null);
            return;
        }

        switch (_hookedFeed)
        {
            case MediaType.Web:
                WebLiveFeed.SourceChanged -= OnWebFeedChanged;
                WebLiveFeed.PointerChanged -= OnWebPointerChanged;
                break;
            case MediaType.Window:
                WindowCaptureFeed.SourceChanged -= OnWebFeedChanged;
                break;
        }

        _webSnapshot = null;
        _hookedFeed = feed;
        switch (feed)
        {
            case MediaType.Web:
                WebLiveFeed.SourceChanged += OnWebFeedChanged;
                WebLiveFeed.PointerChanged += OnWebPointerChanged;
                break;
            case MediaType.Window:
                WindowCaptureFeed.SourceChanged += OnWebFeedChanged;
                break;
        }

        UpdateSnapshotTimer(feed is not null);
    }

    private void UpdateSnapshotTimer(bool wanted)
    {
        var needed = wanted && !IsLiveOutput && PerformanceMode.IsLow;
        if (needed && _webSnapshotTimer is null)
        {
            _webSnapshotTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _webSnapshotTimer.Tick += (_, _) =>
            {
                _webSnapshot = FeedSnapshot();
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

        // Zoomed in, a picture has to be read wider than the screen or it goes
        // soft. Without zoom this stays the 1920 it always was, and the file is
        // not even opened to find out.
        var decodeWidth = media?.Type == MediaType.Image ? ImageLoadCache.DecodeWidthFor(media, path) : ImageDecodeSize.Baseline;

        // A page drawn again keeps its file name but not its contents.
        var stamp = path is not null && !path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && File.Exists(path)
            ? File.GetLastWriteTimeUtc(path).Ticks
            : 0;

        // A new picture that has not been read yet is read on another thread:
        // decoding it here froze the window for 60 to 130 ms right at the
        // change, and the new one came in late. The old one stays on screen
        // (framing and all) until the new one is ready, then this runs again
        // and finds it in the cache.
        var loadVersion = ++_loadVersion;
        _loadPending = false;
        if (media?.Type == MediaType.Image && stamp != 0 && path is not null
            && path != _lastImagePath && path != _loadFailedPath
            && ImageLoadCache.Get(path, decodeWidth, stamp) is null)
        {
            _loadPending = true;
            var pendingPath = path;
            var pendingWidth = decodeWidth;
            var pendingStamp = stamp;
            _ = Task.Run(() => ImageLoadCache.Prepare(pendingPath, pendingWidth, pendingStamp) is not null)
                .ContinueWith(done =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (loadVersion != _loadVersion)
                            return;

                        // Unreadable: carry on, and the old code draws the black fallback.
                        if (!done.Result)
                            _loadFailedPath = pendingPath;
                        OnBackgroundChanged();
                    });
                }, TaskScheduler.Default);
            return;
        }

        // The fill on screen now is about to change: it is kept for the old
        // picture, which may stay underneath while the new one fades in.
        NoteOutgoingFill();
        ApplyFraming();

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

                BitmapImage bitmap;
                if (!isRemote && stamp != 0)
                {
                    // Already read (on another thread, or ahead of time) in nearly
                    // every case; if not, it is read here as it always was.
                    bitmap = ImageLoadCache.Get(path, decodeWidth, stamp)
                             ?? ImageLoadCache.Prepare(path, decodeWidth, stamp)
                             ?? throw new IOException("No se pudo leer la imagen");
                }
                else
                {
                    bitmap = new BitmapImage();
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
                }

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

                    // An image is taking over from what was held for a video
                    // that never came: the fail-safe timer must not let the
                    // held picture go in the middle of this fade.
                    _heldImageTimer?.Stop();
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

                    // What fills the bars of the new picture rises with it, instead of
                    // popping in under the old one.
                    FillImage.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, mediaFade));
                    FillColorLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, mediaFade));
                }
                else if (newPicture)
                {
                    _fadeVersion++;
                    FillImage.BeginAnimation(OpacityProperty, null);
                    FillColorLayer.BeginAnimation(OpacityProperty, null);
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
    /// it is replaced only by one that now dominates the screen (see below);
    /// otherwise it stays, because it is the one the screen still shows.
    /// </summary>
    private void HoldPreviousImage(ImageSource? source, MediaItem? media)
    {
        if (PreviousImage.Visibility == Visibility.Visible)
        {
            // Already holding one (A) while another (B) rises over it, and now
            // a third is about to take B's place. If B is already more than
            // half drawn it is what the screen shows: hold B and A goes, or
            // the next fade would start from A and flash a page that was left
            // behind. B still faint: A is what the screen shows, so it stays
            // (maybe halfway through fading away, back to full strength).
            var incomingDominates = source is not null && BackgroundImage.Opacity >= 0.5;
            if (!incomingDominates)
            {
                PreviousLayer.BeginAnimation(OpacityProperty, null);
                PreviousLayer.Opacity = 1;
                return;
            }
        }
        else if (source is null)
        {
            return;
        }

        PreviousLayer.BeginAnimation(OpacityProperty, null);
        PreviousLayer.Opacity = 1;
        PreviousImage.Source = source;
        _previousMedia = media;
        MediaFraming.Apply(PreviousImage, media, ActualWidth, ActualHeight);
        MediaFraming.SetBlur(PreviousImage, _blurRadius);
        PreviousImage.Visibility = Visibility.Visible;

        // The bars go with the picture they belong to: what filled them a moment
        // ago stays under it, and the new fill rises with the new picture.
        PreviousFillImage.Source = _outgoingFillImage;
        PreviousFillImage.Visibility = _outgoingFillImage is null ? Visibility.Collapsed : Visibility.Visible;
        PreviousFillColor.Fill = _outgoingFillColor;
        PreviousFillColor.Visibility = _outgoingFillColor is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ClearPreviousImage()
    {
        _heldImageTimer?.Stop();
        PreviousLayer.BeginAnimation(OpacityProperty, null);
        PreviousLayer.Opacity = 1;
        PreviousImage.Visibility = Visibility.Collapsed;
        PreviousImage.Source = null;
        PreviousFillImage.Visibility = Visibility.Collapsed;
        PreviousFillImage.Source = null;
        PreviousFillColor.Visibility = Visibility.Collapsed;
        _previousMedia = null;
    }

    /// <summary>What filled the bars just before a change of background: it is held with the old picture.</summary>
    private ImageSource? _outgoingFillImage;

    private Brush? _outgoingFillColor;

    private void NoteOutgoingFill()
    {
        _outgoingFillImage = FillImage.Visibility == Visibility.Visible ? FillImage.Source : null;
        _outgoingFillColor = FillColorLayer.Visibility == Visibility.Visible ? FillColorLayer.Fill : null;
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
        PreviousLayer.BeginAnimation(OpacityProperty, animation);
    }

    /// <summary>
    /// If the video never draws anything, the image is let go after two seconds
    /// (longer when the media fade is: see <see cref="VlcVideoSurface.HoldTimeout"/>).
    /// </summary>
    private void StartHeldImageTimer()
    {
        if (_heldImageTimer is null)
        {
            _heldImageTimer = new System.Windows.Threading.DispatcherTimer();
            _heldImageTimer.Tick += (_, _) => ReleaseHeldImage(TimeSpan.FromMilliseconds(250));
        }

        _heldImageTimer.Interval = VlcVideoSurface.HoldTimeout(TransitionSettings.Current.Media);
        _heldImageTimer.Stop();
        _heldImageTimer.Start();
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
        // On the real output, with the logo or black up, a live page or window is not
        // listened to nor redrawn: nobody can see it.
        var logoCovers = IsLiveOutput && State is OutputState.Logo or OutputState.Black;
        var isFeed = !logoCovers && BackgroundMedia?.Type is MediaType.Web or MediaType.Window;
        HookFeed(isFeed ? BackgroundMedia!.Type : null);

        // Only a video background has frames to show. Anything else (an
        // image, say the offering slide) must never sit under a leftover
        // frame of the video that played before it. A web page and a captured
        // window are the other case: their frames come from the web panel's
        // browser or the window capture, in every view.
        var frame = isFeed
            ? FeedFrame()
            : IsLiveOutput || PerformanceMode.IsLow || _backgroundIsHeavy || BackgroundMedia?.Type != MediaType.Video
                ? null
                : LiveVideoFrame;
        LiveVideo.Source = frame;
        LiveVideo.Visibility = frame is null ? Visibility.Collapsed : Visibility.Visible;

        // The framing (zoom, size, shift) applies to the preview exactly as it
        // does on the projector, so what the operator sees is what goes out.
        MediaFraming.Apply(LiveVideo, frame is null ? null : BackgroundMedia, ActualWidth, ActualHeight);
        MediaFraming.SetBlur(LiveVideo, isFeed ? _blurRadius : 0);
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
        // The new picture is still being read: its framing and fill would be
        // painted on the old one. They are applied when it arrives.
        if (_loadPending)
            return;

        var media = BackgroundMedia;
        MediaFraming.Apply(BackgroundImage, media, ActualWidth, ActualHeight);
        if (PreviousImage.Visibility == Visibility.Visible)
            MediaFraming.Apply(PreviousImage, _previousMedia, ActualWidth, ActualHeight);

        // This layer sits ON TOP of the output window's video surface, so it
        // may only paint the fill when it is the one drawing the picture —
        // otherwise the fill covers the very video it is meant to sit behind.
        var drawsMedia = media is not null && (media.Type is MediaType.Image or MediaType.Web or MediaType.Window || !IsLiveOutput);
        var needsFill = drawsMedia && MediaFraming.NeedsFill(media);
        var fill = needsFill ? FillMedia : null;

        // A video used as fill can only play in the output window, which owns
        // the VLC surfaces; here its poster stands in, exactly like the
        // background video's poster does in the previews.
        var fillPath = needsFill && media is { FillsWithSelf: true }
            ? media.Path
            : fill?.Type == MediaType.Image ? fill.Path : fill?.ThumbnailPath;

        // An image that fills its own bars shows itself, already blurred; any
        // other fill is the picture as it is.
        var selfFill = needsFill && media is { FillsWithSelf: true } && fillPath is not null;
        var fillKey = selfFill ? "blur:" + fillPath : fillPath;

        if (fillKey != _lastFillImagePath)
        {
            _lastFillImagePath = fillKey;
            FillImage.Source = selfFill
                ? MediaFraming.BlurredFill(
                    ImageLoadCache.GetLatest(fillPath!) ?? ImageLoadCache.Prepare(fillPath!, 480, ImageLoadCache.StampOf(fillPath!)))
                : LoadBitmap(fillPath);
        }

        var showFillImage = needsFill && FillImage.Source is not null;
        FillImage.Visibility = showFillImage ? Visibility.Visible : Visibility.Collapsed;

        FillColorLayer.Visibility = needsFill && !showFillImage ? Visibility.Visible : Visibility.Collapsed;
        FillColorLayer.Fill = MediaFraming.FillBrush(media);

        ApplyLiveVideo();
    }


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
