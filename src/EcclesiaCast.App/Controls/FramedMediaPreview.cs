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

    // Layers, bottom to top. The picture is always there; the rest are made
    // the first time they are needed.
    private const int FillLayer = 0;
    private const int PictureLayer = 1;
    private const int BrightnessLayer = 2;
    private const int TintLayer = 3;

    private readonly Image _picture = new()
    {
        Stretch = Stretch.UniformToFill,
        Visibility = Visibility.Collapsed,
        IsHitTestVisible = false,
    };

    private Rectangle? _fillColour;
    private Image? _fillImage;
    private Rectangle? _brightness;
    private Rectangle? _tint;

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

    /// <summary>Loads the picture again (the media changed) and lays it out.</summary>
    private void Refresh()
    {
        var media = Media;
        var path = PathFor(media, UseOriginal);
        var decode = media is not null && path == media.Path ? DecodeWidth : 0;

        _picture.Source = PreviewBitmaps.Get(path, decode);
        _picture.Visibility = _picture.Source is null ? Visibility.Collapsed : Visibility.Visible;
        Reframe();
    }

    /// <summary>Everything that depends on the size or on the framing numbers, not on which file it is.</summary>
    private void Reframe()
    {
        var media = Media;
        MediaFraming.Apply(_picture, media, ActualWidth, ActualHeight);

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
            image.Source = fillSource;
            image.Visibility = Visibility.Visible;
        }
        else if (_fillImage is not null)
        {
            _fillImage.Visibility = Visibility.Collapsed;
        }

        if (MediaFraming.HasColour(media))
        {
            MediaFraming.ApplyColour(EnsureBrightness(), EnsureTint(), media);
        }
        else if (_brightness is not null && _tint is not null)
        {
            _brightness.Visibility = Visibility.Collapsed;
            _tint.Visibility = Visibility.Collapsed;
        }

        ApplyBlur();
    }

    private void ApplyBlur()
    {
        // The output's radius is in pixels of a 1920-wide screen; the same
        // proportion is kept at whatever size this preview has.
        var radius = Math.Clamp(Blur, 0, 100) * 0.6 * (ActualWidth / 1920);
        if (radius < 0.3)
        {
            _picture.Effect = null;
            return;
        }

        if (_picture.Effect is BlurEffect existing && existing.Radius == radius)
            return;

        _picture.Effect = new BlurEffect
        {
            Radius = radius,
            KernelType = KernelType.Gaussian,
            RenderingBias = RenderingBias.Performance,
        };
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

    private Rectangle EnsureBrightness() =>
        _brightness ??= AddLayer(new Rectangle(), BrightnessLayer);

    private Rectangle EnsureTint() =>
        _tint ??= AddLayer(new Rectangle(), TintLayer);

    private T AddLayer<T>(T layer, int z) where T : UIElement
    {
        layer.IsHitTestVisible = false;
        SetZIndex(layer, z);
        Children.Add(layer);
        return layer;
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
