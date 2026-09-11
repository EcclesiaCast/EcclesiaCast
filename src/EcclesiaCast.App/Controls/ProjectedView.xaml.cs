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
        DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(ProjectedView),
            new PropertyMetadata(null, (d, e) => ((ProjectedView)d).SlideView.Highlight = (string?)e.NewValue));

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

    public ProjectedView()
    {
        InitializeComponent();

        // The framing is expressed over a 1920×1080 canvas, so it has to be
        // recomputed whenever this control changes size — the same design
        // shows in a small preview box and on the projector.
        SizeChanged += (_, _) => ApplyFraming();
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

    public string? Highlight
    {
        get => (string?)GetValue(HighlightProperty);
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

    private void ApplyBlur(double amount)
    {
        var radius = Math.Clamp(amount, 0, 100) * 0.6;
        BackgroundImage.Effect = radius <= 0
            ? null
            : new System.Windows.Media.Effects.BlurEffect
            {
                Radius = radius,
                KernelType = System.Windows.Media.Effects.KernelType.Gaussian,
                RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance,
            };
    }

    private void OnBackgroundChanged()
    {
        var media = BackgroundMedia;

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

        if (path == _lastImagePath)
            return;
        _lastImagePath = path;

        // YouTube posters are remote URLs; local media are files on disk.
        var isRemote = path?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true;

        if (!string.IsNullOrWhiteSpace(path) && (isRemote || File.Exists(path)))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1920;
                bitmap.EndInit();
                // A remote poster (YouTube) is still downloading here, and
                // freezing it would throw and drop us into the black fallback.
                // WPF fills the Image in once the download finishes.
                if (bitmap.CanFreeze)
                    bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                BackgroundImage.Visibility = Visibility.Visible;
                if (AnimateTransitions)
                    BackgroundImage.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)) { EasingFunction = new QuadraticEase() });
                return;
            }
            catch
            {
                // Ilegible: se cae al fondo negro.
            }
        }

        BackgroundImage.Source = null;
        BackgroundImage.Visibility = Visibility.Collapsed;
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
        var frame = IsLiveOutput ? null : LiveVideoFrame;
        LiveVideo.Source = frame;
        LiveVideo.Visibility = frame is null ? Visibility.Collapsed : Visibility.Visible;

        // The framing (zoom, size, shift) applies to the preview exactly as it
        // does on the projector, so what the operator sees is what goes out.
        MediaFraming.Apply(LiveVideo, frame is null ? null : BackgroundMedia, ActualWidth, ActualHeight);

        // A poster underneath a live frame is just an older copy of it.
        if (frame is not null)
            BackgroundImage.Visibility = Visibility.Collapsed;
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

        // This layer sits ON TOP of the output window's video surface, so it
        // may only paint the fill when it is the one drawing the picture —
        // otherwise the fill covers the very video it is meant to sit behind.
        var drawsMedia = media is not null && (media.Type == MediaType.Image || !IsLiveOutput);
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
    private void ApplyColourAdjustments(MediaItem? media)
    {
        var brightness = Math.Clamp(media?.Brightness ?? 0, -100, 100);
        if (brightness == 0)
        {
            BrightnessLayer.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Black to darken, white to lighten. Capped short of full cover:
            // the point is to adjust the picture, not to replace it.
            BrightnessLayer.Fill = brightness < 0 ? Brushes.Black : Brushes.White;
            BrightnessLayer.Opacity = Math.Abs(brightness) / 100 * 0.85;
            BrightnessLayer.Visibility = Visibility.Visible;
        }

        var strength = Math.Clamp(media?.TintStrength ?? 0, 0, 100);
        if (strength <= 0 || string.IsNullOrWhiteSpace(media?.Tint))
        {
            TintLayer.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            TintLayer.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(media.Tint));
            TintLayer.Opacity = strength / 100 * 0.85;
            TintLayer.Visibility = Visibility.Visible;
        }
        catch (FormatException)
        {
            TintLayer.Visibility = Visibility.Collapsed;
        }
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
