using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Puts a background picture exactly where the operator wants it on the
/// screen: zoomed in or out, shifted, and — when the church's screen is not
/// the shape of the projector's picture — at a fixed size with the rest of
/// the output filled in.
///
/// Images and videos take different paths to the screen (a WPF image versus
/// frames from VLC), but both end up in an <see cref="Image"/>, so the
/// framing maths lives here once instead of twice.
///
/// «Rellenar» is done over the WHOLE picture. WPF's own UniformToFill crops
/// the overflow first (a layout clip) and the zoom and shift then move that
/// already-cropped piece, so the rest of a tall page could never be shown
/// again. Here the image is always drawn whole (<see cref="Stretch.Uniform"/>
/// never crops) and the cover size is reached with an extra scale in the
/// render transform: the only crop left is the edge of the screen.
/// </summary>
public static class MediaFraming
{
    /// <summary>
    /// How far the shift can go, in screens. A tall page covering the screen
    /// is 2½ screens high and has to be brought up from the bottom, and more
    /// so when zoomed in, so the old ±1 was too short.
    /// </summary>
    public const double MaxOffset = 10;

    /// <summary>What a picture remembers between calls, so a late size or a blur can be redone.</summary>
    private sealed class State
    {
        public MediaItem? Media;
        public double Width;
        public double Height;

        /// <summary>The extra scale «Rellenar» applies over the picture drawn whole (1 when none).</summary>
        public double CoverScale = 1;

        /// <summary>The blur the picture was asked for; the effect's radius is this over <see cref="CoverScale"/>.</summary>
        public double Blur;

        /// <summary>A remote picture whose size is still unknown; re-framed when it arrives.</summary>
        public BitmapSource? Waiting;
    }

    private static readonly ConditionalWeakTable<Image, State> States = new();

    /// <summary>
    /// Sizes and positions <paramref name="picture"/> inside a container of
    /// <paramref name="containerWidth"/> × <paramref name="containerHeight"/>.
    /// Needs the picture's <see cref="Image.Source"/> already set (the cover
    /// scale depends on its shape): call it again when the source changes.
    /// </summary>
    public static void Apply(Image picture, MediaItem? media, double containerWidth, double containerHeight)
    {
        var state = States.GetOrCreateValue(picture);
        state.Media = media;
        state.Width = containerWidth;
        state.Height = containerHeight;

        if (media is null)
        {
            Clear(picture);
            state.CoverScale = 1;
            ApplyBlurEffect(picture, state);
            return;
        }

        // A fixed size is given over the 1920×1080 canvas, so it has to be
        // scaled to whatever the real window or preview box measures.
        var (boxWidth, boxHeight) = FillFraming.BoxSize(
            containerWidth, containerHeight, media.FrameWidth, media.FrameHeight);
        if (media.HasFrame && containerWidth > 0 && containerHeight > 0)
        {
            picture.Width = boxWidth;
            picture.Height = boxHeight;
            picture.HorizontalAlignment = HorizontalAlignment.Center;
            picture.VerticalAlignment = VerticalAlignment.Center;
        }
        else
        {
            picture.Width = double.NaN;
            picture.Height = double.NaN;
            picture.HorizontalAlignment = HorizontalAlignment.Stretch;
            picture.VerticalAlignment = VerticalAlignment.Stretch;
        }

        var zoom = Math.Clamp(media.Zoom <= 0 ? 1 : media.Zoom, 0.1, 4);
        var offsetX = Math.Clamp(media.OffsetX, -MaxOffset, MaxOffset) * containerWidth;
        var offsetY = Math.Clamp(media.OffsetY, -MaxOffset, MaxOffset) * containerHeight;

        if (media.Scaling == MediaScaling.Fill
            && PictureSize(picture, state) is { } size
            && boxWidth > 0 && boxHeight > 0)
        {
            ApplyFill(picture, state, media, size, containerWidth, containerHeight, boxWidth, boxHeight, zoom, offsetX, offsetY);
            return;
        }

        // Fit and Stretch never crop, and a Fill whose picture is not known
        // yet (a video before its first frame, a poster still downloading)
        // stays as WPF draws it until the size arrives.
        picture.Clip = null;
        state.CoverScale = 1;
        ApplyBlurEffect(picture, state);
        picture.Stretch = media.Scaling switch
        {
            MediaScaling.Fit => Stretch.Uniform,
            MediaScaling.Stretch => Stretch.Fill,
            _ => Stretch.UniformToFill,
        };

        SetTransform(picture, zoom, offsetX, offsetY);
    }

    /// <summary>
    /// «Rellenar»: the picture covers the box, centred on it (WPF's own
    /// UniformToFill anchors it at the top-left corner instead), and zooming
    /// works from the middle of the screen. With a fixed size the box
    /// stays where it is: it is a window onto the picture, which moves and
    /// zooms behind it.
    /// </summary>
    private static void ApplyFill(
        Image picture, State state, MediaItem media, Size source,
        double containerWidth, double containerHeight, double boxWidth, double boxHeight,
        double zoom, double offsetX, double offsetY)
    {
        var place = FillFraming.Place(
            source.Width, source.Height, containerWidth, containerHeight,
            boxWidth, boxHeight, media.HasFrame, zoom, offsetX, offsetY);
        state.CoverScale = place.CoverScale;

        // Drawn whole and centred in its box: this is what Uniform does, and
        // it never crops. A picture already the shape of the box has nothing
        // to crop either way, and is left to WPF exactly as it always was.
        picture.Stretch = place.CoverScale - 1 < 1e-9 ? Stretch.UniformToFill : Stretch.Uniform;

        // The fixed-size window is carried by the render transform too, so it
        // is clipped in the picture's own coordinates to stay put on screen.
        picture.Clip = place.Window is { } window
            ? new RectangleGeometry(new Rect(window.X, window.Y, window.Width, window.Height))
            : null;

        ApplyBlurEffect(picture, state);
        SetTransform(picture, place.Scale, place.ShiftX, place.ShiftY);
    }

    private static void SetTransform(Image picture, double scale, double shiftX, double shiftY)
    {
        if (Math.Abs(scale - 1) < 0.001 && Math.Abs(shiftX) < 0.0001 && Math.Abs(shiftY) < 0.0001)
        {
            picture.RenderTransform = Transform.Identity;
            return;
        }

        picture.RenderTransformOrigin = new Point(0.5, 0.5);
        picture.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(scale, scale),
                new TranslateTransform(shiftX, shiftY),
            },
        };
    }

    /// <summary>
    /// The shape of what the image shows, or null while it is not known. A
    /// picture still downloading reports no size: it is framed again the
    /// moment it arrives.
    /// </summary>
    private static Size? PictureSize(Image picture, State state)
    {
        if (picture.Source is not { } source)
            return null;

        if (source is BitmapSource { IsDownloading: true } downloading)
        {
            if (!ReferenceEquals(state.Waiting, downloading))
            {
                state.Waiting = downloading;
                downloading.DownloadCompleted += (_, _) =>
                {
                    if (ReferenceEquals(picture.Source, downloading))
                        Apply(picture, state.Media, state.Width, state.Height);
                };
            }
            return null;
        }

        return source.Width > 0 && source.Height > 0 ? new Size(source.Width, source.Height) : null;
    }

    /// <summary>
    /// The rectangle a picture takes on the screen with no zoom and no shift,
    /// in the same units as <paramref name="containerWidth"/> — what the
    /// properties window needs to show what lies off screen. Only «Rellenar»
    /// goes past the screen; the other modes stay inside it.
    /// </summary>
    public static Rect NaturalBounds(MediaItem? media, Size? source, double containerWidth, double containerHeight)
    {
        var screen = new Rect(0, 0, containerWidth, containerHeight);
        if (media is not { Scaling: MediaScaling.Fill } || source is not { Width: > 0, Height: > 0 } size)
            return screen;

        var (boxWidth, boxHeight) = FillFraming.BoxSize(
            containerWidth, containerHeight, media.FrameWidth, media.FrameHeight);
        var bounds = FillFraming.NaturalBounds(
            size.Width, size.Height, containerWidth, containerHeight, boxWidth, boxHeight);
        return new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Blurs the picture. Goes through here, not straight to the effect: the
    /// render transform scales the effect along with the picture, so the
    /// radius is divided by the cover scale and the blur looks the same as it
    /// did when the picture was simply drawn at cover size.
    /// </summary>
    public static void SetBlur(Image picture, double radius)
    {
        var state = States.GetOrCreateValue(picture);
        state.Blur = radius;
        ApplyBlurEffect(picture, state);
    }

    private static void ApplyBlurEffect(Image picture, State state)
    {
        var radius = state.Blur / state.CoverScale;
        if (state.Blur <= 0 || radius < 0.05)
        {
            picture.Effect = null;
            return;
        }

        if (picture.Effect is BlurEffect existing && Math.Abs(existing.Radius - radius) < 0.0001)
            return;

        picture.Effect = new BlurEffect
        {
            Radius = radius,
            KernelType = KernelType.Gaussian,
            RenderingBias = RenderingBias.Performance,
        };
    }

    /// <summary>
    /// Gives the fill picture its look: soft and a little enlarged when the
    /// bars are painted with a copy of the image itself (the enlargement keeps
    /// the blur's faded edge off-screen), as it comes otherwise. The radius is
    /// in pixels of a 1920-wide screen, kept in proportion at any size.
    /// </summary>
    public static void SetFillLook(Image fill, MediaItem? media, double screenWidth, double screenHeight)
    {
        if (media is null || !media.FillsWithSelf || screenWidth <= 0 || screenHeight <= 0)
        {
            fill.Effect = null;
            fill.ClearValue(FrameworkElement.WidthProperty);
            fill.ClearValue(FrameworkElement.HeightProperty);
            fill.RenderTransform = Transform.Identity;
            return;
        }

        // The blur fades to transparent at the edge of the picture, so the
        // picture is made bigger than the screen (and centered) until that
        // faded edge falls off it. It is made bigger itself, not scaled by a
        // render transform: a blur under a scale transform leaves a black
        // strip along the bottom edge (measured: 48 rows at 1920×1080).
        const double Oversize = 1.4;
        fill.RenderTransform = Transform.Identity;
        fill.HorizontalAlignment = HorizontalAlignment.Center;
        fill.VerticalAlignment = VerticalAlignment.Center;
        fill.Width = screenWidth * Oversize;
        fill.Height = screenHeight * Oversize;

        var radius = 40 * (screenWidth / 1920);
        if (fill.Effect is not BlurEffect existing || Math.Abs(existing.Radius - radius) > 0.01)
        {
            fill.Effect = new BlurEffect
            {
                Radius = radius,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance,
            };
        }
    }

    /// <summary>True when the picture leaves part of the output uncovered.</summary>
    public static bool NeedsFill(MediaItem? media) =>
        media is not null
        && (media.HasFrame
            || media.Zoom is > 0 and < 1
            || media.Scaling == MediaScaling.Fit
            || media.OffsetX != 0
            || media.OffsetY != 0);

    public static Brush FillBrush(MediaItem? media)
    {
        var hex = media?.FillColor;
        if (string.IsNullOrWhiteSpace(hex))
            return Brushes.Black;

        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch (FormatException)
        {
            return Brushes.Black;
        }
    }

    /// <summary>
    /// Darkening and tinting: two flat layers laid over the picture. The
    /// projector's view and the small previews of the media bar share this, so
    /// a tinted background looks the same wherever it is drawn.
    /// </summary>
    public static void ApplyColour(Rectangle brightnessLayer, Rectangle tintLayer, MediaItem? media)
    {
        var brightness = Math.Clamp(media?.Brightness ?? 0, -100, 100);
        if (brightness == 0)
        {
            brightnessLayer.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Black to darken, white to lighten. Capped short of full cover:
            // the point is to adjust the picture, not to replace it.
            brightnessLayer.Fill = brightness < 0 ? Brushes.Black : Brushes.White;
            brightnessLayer.Opacity = Math.Abs(brightness) / 100 * 0.85;
            brightnessLayer.Visibility = Visibility.Visible;
        }

        var strength = Math.Clamp(media?.TintStrength ?? 0, 0, 100);
        if (strength <= 0 || string.IsNullOrWhiteSpace(media?.Tint))
        {
            tintLayer.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            tintLayer.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(media.Tint));
            tintLayer.Opacity = strength / 100 * 0.85;
            tintLayer.Visibility = Visibility.Visible;
        }
        catch (FormatException)
        {
            tintLayer.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>True when darkening or tinting would show anything at all.</summary>
    public static bool HasColour(MediaItem? media) =>
        media is not null
        && (media.Brightness != 0
            || (media.TintStrength > 0 && !string.IsNullOrWhiteSpace(media.Tint)));

    private static void Clear(Image picture)
    {
        picture.Width = double.NaN;
        picture.Height = double.NaN;
        picture.HorizontalAlignment = HorizontalAlignment.Stretch;
        picture.VerticalAlignment = VerticalAlignment.Stretch;
        picture.RenderTransform = Transform.Identity;
        picture.Clip = null;
    }
}
