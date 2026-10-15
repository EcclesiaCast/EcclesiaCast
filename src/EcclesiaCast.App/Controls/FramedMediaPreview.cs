using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// A media item drawn the way it will come out on the screen: scaled, zoomed,
/// shifted and at its fixed size, over its fill colour or fill picture, with
/// its brightness and tint. The cards of the media bar and the properties
/// window both show it, so what you see there is what <see cref="ProjectedView"/>
/// projects — the maths is the same <see cref="MediaFraming"/>.
///
/// Videos and YouTube show their poster; there is no moving picture here.
/// It is built to be cheap in the hundreds: the fill and colour layers only
/// exist once a media actually needs them, and the pictures come from a small
/// shared cache.
/// </summary>
public sealed class FramedMediaPreview : Grid
{
    public static readonly DependencyProperty MediaProperty =
        DependencyProperty.Register(nameof(Media), typeof(MediaItem), typeof(FramedMediaPreview),
            new PropertyMetadata(null, (d, _) => ((FramedMediaPreview)d).Refresh()));

    /// <summary>What sits behind the picture where it does not reach, when the media asks for another item.</summary>
    public static readonly DependencyProperty FillMediaProperty =
        DependencyProperty.Register(nameof(FillMedia), typeof(MediaItem), typeof(FramedMediaPreview),
            new PropertyMetadata(null, (d, _) => ((FramedMediaPreview)d).Reframe()));

    /// <summary>
    /// Draw an image from its own file rather than from the small poster the
    /// library keeps for it. The properties window asks for that; the cards
    /// do not, so a tab of hundreds never decodes a full-size picture.
    /// </summary>
    public static readonly DependencyProperty UseOriginalProperty =
        DependencyProperty.Register(nameof(UseOriginal), typeof(bool), typeof(FramedMediaPreview),
            new PropertyMetadata(false, (d, _) => ((FramedMediaPreview)d).Refresh()));

    /// <summary>Width to decode an original image at (0 = as big as the file). Posters are small and never re-scaled.</summary>
    public static readonly DependencyProperty DecodeWidthProperty =
        DependencyProperty.Register(nameof(DecodeWidth), typeof(int), typeof(FramedMediaPreview),
            new PropertyMetadata(0, (d, _) => ((FramedMediaPreview)d).Refresh()));

    /// <summary>
    /// Blur of the picture, 0–100, as the output applies it. Not read from the
    /// media: the cards leave it at 0 (it costs and cannot be told apart at
    /// that size) while the properties window passes what is being edited.
    /// </summary>
    public static readonly DependencyProperty BlurProperty =
        DependencyProperty.Register(nameof(Blur), typeof(double), typeof(FramedMediaPreview),
            new PropertyMetadata(0d, (d, _) => ((FramedMediaPreview)d).Reframe()));

    /// <summary>
    /// Also draw what lies off the screen: the whole picture, with the part
    /// outside the screen darkened and the screen's outline marked, like a
    /// photo cropper. The properties window asks for it, so the operator can
    /// see which part of a page goes out and where to move it; the cards of
    /// the media bar do not, they only show what is on screen. The control
    /// then needs room around the screen rectangle, and makes it itself.
    /// </summary>
    public static readonly DependencyProperty ShowOutsideProperty =
        DependencyProperty.Register(nameof(ShowOutside), typeof(bool), typeof(FramedMediaPreview),
            new PropertyMetadata(false, (d, _) => ((FramedMediaPreview)d).ShowOutsideChanged()));

    /// <summary>The canvas the framing numbers are expressed over.</summary>
    private const double CanvasWidth = 1920;
    private const double CanvasHeight = 1080;

    // Layers, bottom to top. The picture is always there; the rest are made
    // the first time they are needed.
    private const int FillLayer = 0;
    private const int PictureLayer = 1;

    private readonly Image _picture = new()
    {
        Stretch = Stretch.UniformToFill,
        Visibility = Visibility.Collapsed,
        IsHitTestVisible = false,
    };

    // Only with ShowOutside: the screen rectangle holds every layer, and the
    // scrim and the outline sit above it in this control.
    private Grid? _stage;
    private System.Windows.Shapes.Path? _scrim;
    private Rectangle? _outline;
    private Rectangle? _outlineShadow;
    private Rect _screen;

    private Rectangle? _fillColour;
    private Image? _fillImage;

    public FramedMediaPreview()
    {
        ClipToBounds = true;
        SetZIndex(_picture, PictureLayer);
        Children.Add(_picture);

        // The framing is expressed over a 1920×1080 canvas, so it has to be
        // redone for whatever size the preview ends up with.
        SizeChanged += (_, _) => Reframe();
    }

    public MediaItem? Media
    {
        get => (MediaItem?)GetValue(MediaProperty);
        set => SetValue(MediaProperty, value);
    }

    public MediaItem? FillMedia
    {
        get => (MediaItem?)GetValue(FillMediaProperty);
        set => SetValue(FillMediaProperty, value);
    }

    public bool UseOriginal
    {
        get => (bool)GetValue(UseOriginalProperty);
        set => SetValue(UseOriginalProperty, value);
    }

    public int DecodeWidth
    {
        get => (int)GetValue(DecodeWidthProperty);
        set => SetValue(DecodeWidthProperty, value);
    }

    public double Blur
    {
        get => (double)GetValue(BlurProperty);
        set => SetValue(BlurProperty, value);
    }

    public bool ShowOutside
    {
        get => (bool)GetValue(ShowOutsideProperty);
        set => SetValue(ShowOutsideProperty, value);
    }

    /// <summary>Width of the screen rectangle inside this control (the whole control when it shows nothing off screen).</summary>
    public double ScreenWidth => _stage is not null && _screen.Width > 0 ? _screen.Width : ActualWidth;

    /// <summary>Height of the screen rectangle inside this control.</summary>
    public double ScreenHeight => _stage is not null && _screen.Height > 0 ? _screen.Height : ActualHeight;

    /// <summary>Loads the picture again (the media changed) and lays it out.</summary>
    private void Refresh()
    {
        var media = Media;
        var path = PathFor(media, UseOriginal);
        var decode = media is not null && path == media.Path ? DecodeWidth : 0;

        _picture.Source = PreviewBitmaps.Get(path, decode);
        _picture.Visibility = _picture.Source is null ? Visibility.Collapsed : Visibility.Visible;

        // A poster still downloading has no shape yet; it is laid out again
        // when it arrives.
        if (_picture.Source is BitmapSource { IsDownloading: true } downloading)
            downloading.DownloadCompleted += (_, _) =>
            {
                if (ReferenceEquals(_picture.Source, downloading))
                    Reframe();
            };

        Reframe();
    }

    /// <summary>Everything that depends on the size or on the framing numbers, not on which file it is.</summary>
    private void Reframe()
    {
        var media = Media;
        Relayout();
        MediaFraming.Apply(_picture, media, ScreenWidth, ScreenHeight);

        // Same rule as the projector: the fill is painted only where the
        // picture leaves the screen uncovered.
        var needsFill = _picture.Source is not null && MediaFraming.NeedsFill(media);
        var fillSource = needsFill && FillMedia is { } fill
            ? PreviewBitmaps.Get(PathFor(fill, UseOriginal), UseOriginal && fill.Type == MediaType.Image ? DecodeWidth : 0)
            : null;

        if (needsFill && fillSource is null)
        {
            var colour = EnsureFillColour();
            colour.Fill = MediaFraming.FillBrush(media);
            colour.Visibility = Visibility.Visible;
        }
        else if (_fillColour is not null)
        {
            _fillColour.Visibility = Visibility.Collapsed;
        }

        if (fillSource is not null)
        {
            var image = EnsureFillImage();
            image.Source = media is { FillsWithSelf: true } ? MediaFraming.BlurredFill(fillSource as BitmapSource) ?? fillSource : fillSource;
            image.Visibility = Visibility.Visible;
        }
        else if (_fillImage is not null)
        {
            _fillImage.Visibility = Visibility.Collapsed;
        }

        ApplyBlur();
    }

    private void ApplyBlur()
    {
        // The output's radius is in pixels of a 1920-wide screen; the same
        // proportion is kept at whatever size this preview has.
        var radius = Math.Clamp(Blur, 0, 100) * 0.6 * (ScreenWidth / 1920);

        // Through the framing, which knows how much it scales the picture.
        MediaFraming.SetBlur(_picture, radius < 0.3 ? 0 : radius);
    }

    /// <summary>
    /// The still to draw: the poster for anything that moves, the file itself
    /// for an image when it is asked for, and the poster otherwise (it is
    /// already small, and for an image it is the file when the shell had none).
    /// </summary>
    private static string? PathFor(MediaItem? media, bool original)
    {
        if (media is null)
            return null;

        if (original && media.Type == MediaType.Image)
            return media.Path;

        if (!string.IsNullOrWhiteSpace(media.ThumbnailPath))
            return media.ThumbnailPath;

        return media.Type == MediaType.Image ? media.Path : null;
    }

    private Rectangle EnsureFillColour() =>
        _fillColour ??= AddLayer(new Rectangle(), FillLayer);

    private Image EnsureFillImage() =>
        _fillImage ??= AddLayer(new Image { Stretch = Stretch.UniformToFill }, FillLayer);

    private T AddLayer<T>(T layer, int z) where T : UIElement
    {
        layer.IsHitTestVisible = false;
        SetZIndex(layer, z);
        (_stage ?? (Panel)this).Children.Add(layer);
        return layer;
    }

    // ── Lo que queda fuera de la pantalla ────────────────────────

    private void ShowOutsideChanged()
    {
        if (ShowOutside == (_stage is not null))
            return;

        if (ShowOutside)
        {
            // Every layer moves into the screen rectangle; this control keeps
            // only that, the darkening around it and its outline.
            var layers = Children.Cast<UIElement>().ToList();
            Children.Clear();
            _stage = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            foreach (var layer in layers)
                _stage.Children.Add(layer);

            _scrim = new System.Windows.Shapes.Path { Fill = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), IsHitTestVisible = false };
            _outlineShadow = NewOutline(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), 3.5);
            _outline = NewOutline(Brushes.White, 1.5);
            SetZIndex(_scrim, 1);
            SetZIndex(_outlineShadow, 2);
            SetZIndex(_outline, 3);

            Children.Add(_stage);
            Children.Add(_scrim);
            Children.Add(_outlineShadow);
            Children.Add(_outline);
        }
        else
        {
            var layers = _stage!.Children.Cast<UIElement>().ToList();
            _stage.Children.Clear();
            Children.Clear();
            foreach (var layer in layers)
                Children.Add(layer);
            _stage = null;
            _scrim = null;
            _outline = null;
            _outlineShadow = null;
            _screen = default;
        }

        Reframe();
    }

    private static Rectangle NewOutline(Brush stroke, double thickness) => new()
    {
        Stroke = stroke,
        StrokeThickness = thickness,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
    };

    /// <summary>
    /// Works out where the screen rectangle goes. The scale is the one at
    /// which the picture, drawn as «Rellenar» leaves it with no zoom and no
    /// shift, fits whole in the control with some room around — so the view
    /// does not jump while zoom and shift are being edited, and a tall page
    /// is seen from top to bottom. Zooming in then pushes the picture past
    /// the edges, which is the point.
    /// </summary>
    private void Relayout()
    {
        if (_stage is null || _scrim is null || _outline is null || _outlineShadow is null)
            return;

        double width = ActualWidth, height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        Size? shape = _picture.Source is { Width: > 0, Height: > 0 } source ? new Size(source.Width, source.Height) : null;
        var natural = MediaFraming.NaturalBounds(Media, shape, CanvasWidth, CanvasHeight);

        // A hopelessly long strip must not shrink the screen to a speck.
        var world = Rect.Intersect(
            Rect.Union(new Rect(0, 0, CanvasWidth, CanvasHeight), natural),
            new Rect(-1.5 * CanvasWidth, -1.5 * CanvasHeight, 4 * CanvasWidth, 4 * CanvasHeight));
        world.Inflate(CanvasWidth * 0.06, CanvasWidth * 0.06);

        var scale = Math.Min(width / world.Width, height / world.Height);
        _screen = new Rect(
            (width - world.Width * scale) / 2 - world.X * scale,
            (height - world.Height * scale) / 2 - world.Y * scale,
            CanvasWidth * scale,
            CanvasHeight * scale);

        _stage.Margin = new Thickness(_screen.X, _screen.Y, 0, 0);
        _stage.Width = _screen.Width;
        _stage.Height = _screen.Height;

        _scrim.Data = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, width, height)),
            new RectangleGeometry(_screen));

        foreach (var line in new[] { _outline, _outlineShadow })
        {
            line.Margin = new Thickness(_screen.X, _screen.Y, 0, 0);
            line.Width = _screen.Width;
            line.Height = _screen.Height;
        }
    }
}

/// <summary>
/// The small pictures the media bar draws, kept by weak reference: a tab is
/// rebuilt every time the library changes, and without this every card would
/// decode its poster again even though nothing about the file moved.
/// </summary>
internal static class PreviewBitmaps
{
    private static readonly Dictionary<(string Path, long Stamp, int Width), WeakReference<BitmapSource>> Cache = [];

    public static BitmapSource? Get(string? path, int decodeWidth)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // YouTube posters are remote: WPF downloads and caches them itself,
        // and they cannot be frozen until they arrive.
        var remote = path.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        long stamp = 0;
        if (!remote)
        {
            // One stat gives both "is it there" and "did it change": an
            // imported page that is drawn again at a better quality keeps its
            // name but must not keep its old picture.
            var info = new FileInfo(path);
            if (!info.Exists)
                return null;
            stamp = info.LastWriteTimeUtc.Ticks;
        }

        var key = (path, stamp, decodeWidth);
        if (Cache.TryGetValue(key, out var weak) && weak.TryGetTarget(out var cached))
            return cached;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = remote ? BitmapCacheOption.Default : BitmapCacheOption.OnLoad;
            if (decodeWidth > 0)
                bitmap.DecodePixelWidth = decodeWidth;
            bitmap.EndInit();
            if (bitmap.CanFreeze)
                bitmap.Freeze();

            if (Cache.Count > 600)
                Prune();
            Cache[key] = new WeakReference<BitmapSource>(bitmap);
            return bitmap;
        }
        catch
        {
            // Unreadable: the card stays empty, as it always did.
            return null;
        }
    }

    private static void Prune()
    {
        foreach (var dead in Cache.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToList())
            Cache.Remove(dead);
    }
}
