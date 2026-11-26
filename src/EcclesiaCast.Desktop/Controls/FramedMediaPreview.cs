using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Desktop.Services;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// A media item drawn the way it will come out on the screen: scaled, zoomed,
/// shifted and at its fixed size, over its fill colour or fill picture, with
/// its blur. The Avalonia side of the WPF <c>FramedMediaPreview</c>, with the
/// same <see cref="MediaFraming"/> as <see cref="ProjectedView"/>, so what the
/// cards and the logos show is what projects.
///
/// Videos show their poster; there is no moving picture here. It is built to
/// be cheap in the hundreds: the fill layers only exist once a media needs
/// them, and the pictures come from the shared <see cref="Pictures"/> cache.
///
/// Left for phase 3, where the properties window comes: <c>ShowOutside</c>
/// (the whole picture with what falls off the screen darkened, like a photo
/// cropper) and its <c>ScreenWidth</c>/<c>ScreenHeight</c>.
/// </summary>
public sealed class FramedMediaPreview : Grid
{
    public static readonly StyledProperty<MediaItem?> MediaProperty =
        AvaloniaProperty.Register<FramedMediaPreview, MediaItem?>(nameof(Media));

    /// <summary>What sits behind the picture where it does not reach, when the media asks for another item.</summary>
    public static readonly StyledProperty<MediaItem?> FillMediaProperty =
        AvaloniaProperty.Register<FramedMediaPreview, MediaItem?>(nameof(FillMedia));

    /// <summary>
    /// Draw an image from its own file rather than from the small poster the
    /// library keeps for it (the properties window; the cards never do).
    /// </summary>
    public static readonly StyledProperty<bool> UseOriginalProperty =
        AvaloniaProperty.Register<FramedMediaPreview, bool>(nameof(UseOriginal));

    /// <summary>Width to read an original image at (0 = as big as the file). Posters are small and never re-scaled.</summary>
    public static readonly StyledProperty<int> DecodeWidthProperty =
        AvaloniaProperty.Register<FramedMediaPreview, int>(nameof(DecodeWidth));

    /// <summary>Blur of the picture, 0–100, as the output applies it (the cards leave it at 0).</summary>
    public static readonly StyledProperty<double> BlurProperty =
        AvaloniaProperty.Register<FramedMediaPreview, double>(nameof(Blur));

    private readonly Image _picture = new()
    {
        Stretch = Stretch.UniformToFill,
        IsVisible = false,
        IsHitTestVisible = false,
    };

    private Rectangle? _fillColour;
    private Image? _fillImage;
    private Size _arranged;

    public FramedMediaPreview()
    {
        ClipToBounds = true;
        Children.Add(_picture);
    }

    public MediaItem? Media { get => GetValue(MediaProperty); set => SetValue(MediaProperty, value); }
    public MediaItem? FillMedia { get => GetValue(FillMediaProperty); set => SetValue(FillMediaProperty, value); }
    public bool UseOriginal { get => GetValue(UseOriginalProperty); set => SetValue(UseOriginalProperty, value); }
    public int DecodeWidth { get => GetValue(DecodeWidthProperty); set => SetValue(DecodeWidthProperty, value); }
    public double Blur { get => GetValue(BlurProperty); set => SetValue(BlurProperty, value); }

    /// <summary>The picture (for the tests).</summary>
    internal Image Picture => _picture;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MediaProperty || change.Property == UseOriginalProperty || change.Property == DecodeWidthProperty)
            Refresh();
        else if (change.Property == FillMediaProperty || change.Property == BlurProperty)
            Reframe();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        // The framing is expressed over a 1920×1080 canvas, so it is redone
        // for whatever size the preview ends up with.
        if (finalSize != _arranged)
        {
            _arranged = finalSize;
            Reframe();
        }

        return arranged;
    }

    /// <summary>Loads the picture again (the media changed) and lays it out.</summary>
    private void Refresh()
    {
        var media = Media;
        var path = PathFor(media, UseOriginal);
        _picture.Source = Pictures.Load(path, WidthFor(media, path, DecodeWidth));
        _picture.IsVisible = _picture.Source is not null;
        Reframe();
    }

    /// <summary>Everything that depends on the size or on the framing numbers, not on which file it is.</summary>
    private void Reframe()
    {
        var media = Media;
        MediaFraming.Apply(_picture, media, _arranged.Width, _arranged.Height);

        // Same rule as the projector: the fill is painted only where the
        // picture leaves the screen uncovered.
        var needsFill = _picture.Source is not null && MediaFraming.NeedsFill(media);
        var fillPath = needsFill && FillMedia is { } fill ? PathFor(fill, UseOriginal) : null;
        var fillSource = fillPath is null ? null
            : Pictures.Load(fillPath, WidthFor(FillMedia, fillPath, UseOriginal ? DecodeWidth : 0));

        if (needsFill && fillSource is null)
        {
            var colour = _fillColour ??= AddFillLayer(new Rectangle());
            colour.Fill = MediaFraming.FillBrush(media);
            colour.IsVisible = true;
        }
        else if (_fillColour is not null)
        {
            _fillColour.IsVisible = false;
        }

        if (fillSource is not null)
        {
            var image = _fillImage ??= AddFillLayer(new Image { Stretch = Stretch.UniformToFill });
            image.Source = media is { FillsWithSelf: true } or { FillsWithOwnVideo: true }
                ? MediaFraming.BlurredFill(fillSource) ?? fillSource
                : fillSource;
            image.IsVisible = true;
        }
        else if (_fillImage is not null)
        {
            _fillImage.IsVisible = false;
        }

        // The output's radius is in pixels of a 1920-wide screen; the same
        // proportion is kept at whatever size this preview has.
        var radius = Math.Clamp(Blur, 0, 100) * 0.6 * (_arranged.Width / 1920);
        MediaFraming.SetBlur(_picture, radius < 0.3 ? 0 : radius);
    }

    /// <summary>The fill layers go under the picture, made the first time they are needed.</summary>
    private T AddFillLayer<T>(T layer) where T : Control
    {
        layer.IsHitTestVisible = false;
        Children.Insert(0, layer);
        return layer;
    }

    /// <summary>
    /// The still to draw: the poster for anything that moves, the file itself
    /// for an image when it is asked for, and the poster otherwise.
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

    /// <summary>
    /// The file of the media at the width asked for; a poster as it is (it
    /// is small already, and is never scaled up), as in WPF.
    /// </summary>
    private static int WidthFor(MediaItem? media, string? path, int decodeWidth) =>
        media is not null && path == media.Path ? decodeWidth : 0;
}
