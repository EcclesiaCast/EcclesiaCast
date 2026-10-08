using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
/// </summary>
public static class MediaFraming
{
    /// <summary>The canvas the framing numbers are expressed over.</summary>
    private const double CanvasWidth = 1920;
    private const double CanvasHeight = 1080;

    /// <summary>
    /// Sizes and positions <paramref name="picture"/> inside a container of
    /// <paramref name="containerWidth"/> × <paramref name="containerHeight"/>.
    /// </summary>
    public static void Apply(Image picture, MediaItem? media, double containerWidth, double containerHeight)
    {
        if (media is null)
        {
            Clear(picture);
            return;
        }

        picture.Stretch = media.Scaling switch
        {
            MediaScaling.Fit => Stretch.Uniform,
            MediaScaling.Stretch => Stretch.Fill,
            _ => Stretch.UniformToFill,
        };

        // A fixed size is given over the 1920×1080 canvas, so it has to be
        // scaled to whatever the real window or preview box measures.
        if (media.HasFrame && containerWidth > 0 && containerHeight > 0)
        {
            picture.Width = Math.Min(containerWidth, media.FrameWidth!.Value * containerWidth / CanvasWidth);
            picture.Height = Math.Min(containerHeight, media.FrameHeight!.Value * containerHeight / CanvasHeight);
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
        var offsetX = Math.Clamp(media.OffsetX, -1, 1) * containerWidth;
        var offsetY = Math.Clamp(media.OffsetY, -1, 1) * containerHeight;

        if (Math.Abs(zoom - 1) < 0.001 && offsetX == 0 && offsetY == 0)
        {
            picture.RenderTransform = Transform.Identity;
            return;
        }

        picture.RenderTransformOrigin = new Point(0.5, 0.5);
        picture.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(zoom, zoom),
                new TranslateTransform(offsetX, offsetY),
            },
        };
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
    }
}
