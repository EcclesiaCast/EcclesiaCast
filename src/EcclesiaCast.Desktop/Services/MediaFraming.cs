using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// Puts a picture exactly where the operator wants it on the screen: zoomed,
/// shifted and at a fixed size, the Avalonia side of the WPF
/// <c>App/Services/MediaFraming</c>. The numbers come from the shared
/// <see cref="FramingNumbers"/> and <see cref="FillFraming"/>; this only turns
/// them into a size, a transform, a clip and a blur on an <see cref="Image"/>.
/// Images, the frames of a video and the logo all go through here.
///
/// «Rellenar» is done over the WHOLE picture: it is drawn whole
/// (<see cref="Stretch.Uniform"/> never crops) and the cover size is reached
/// with an extra scale in the render transform, so the only crop left is the
/// edge of the screen. (Avalonia's own UniformToFill crops the overflow, like
/// WPF's, although from the middle rather than from the top-left corner.)
/// </summary>
public static class MediaFraming
{
    private sealed class State
    {
        public MediaItem? Media;
        public double Width;
        public double Height;

        /// <summary>The extra scale «Rellenar» applies over the picture drawn whole (1 when none).</summary>
        public double CoverScale = 1;

        /// <summary>The blur asked for; the effect's radius is this over <see cref="CoverScale"/>.</summary>
        public double Blur;
    }

    private static readonly ConditionalWeakTable<Image, State> States = new();

    /// <summary>
    /// Sizes and positions <paramref name="picture"/> inside a container of
    /// <paramref name="containerWidth"/> × <paramref name="containerHeight"/>.
    /// Needs the picture's source already set (the cover scale depends on its
    /// shape): call it again when the source changes.
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

        // A fixed size is given over the 1920×1080 canvas.
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

        var zoom = FramingNumbers.Zoom(media);
        var (offsetX, offsetY) = FramingNumbers.Offset(media, containerWidth, containerHeight);

        if (media.Scaling == MediaScaling.Fill
            && PictureSize(picture) is { } size
            && boxWidth > 0 && boxHeight > 0)
        {
            var place = FillFraming.Place(
                size.Width, size.Height, containerWidth, containerHeight,
                boxWidth, boxHeight, media.HasFrame, zoom, offsetX, offsetY);
            state.CoverScale = place.CoverScale;

            // Drawn whole and centred in its box. A picture already the shape
            // of the box has nothing to crop either way.
            picture.Stretch = place.CoverScale - 1 < 1e-9 ? Stretch.UniformToFill : Stretch.Uniform;

            // The fixed-size window is carried by the render transform too, so
            // it is clipped in the picture's own coordinates to stay put.
            picture.Clip = place.Window is { } window
                ? new RectangleGeometry(new Rect(window.X, window.Y, window.Width, window.Height))
                : null;

            ApplyBlurEffect(picture, state);
            SetTransform(picture, place.Scale, place.ShiftX, place.ShiftY);
            return;
        }

        // Fit and Stretch never crop, and a Fill whose picture is not known yet
        // (a video before its first frame) stays as it is drawn until then.
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

    private static void SetTransform(Image picture, double scale, double shiftX, double shiftY)
    {
        if (Math.Abs(scale - 1) < 0.001 && Math.Abs(shiftX) < 0.0001 && Math.Abs(shiftY) < 0.0001)
        {
            picture.RenderTransform = null;
            return;
        }

        picture.RenderTransformOrigin = RelativePoint.Center;
        picture.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(scale, scale),
                new TranslateTransform(shiftX, shiftY),
            },
        };
    }

    /// <summary>The shape of what the image shows, or null while it is not known.</summary>
    private static Size? PictureSize(Image picture) =>
        picture.Source is { Size: { Width: > 0, Height: > 0 } size } ? size : null;

    /// <summary>
    /// The rectangle a picture takes on the screen with no zoom and no shift,
    /// in the units of the container. Only «Rellenar» goes past the screen.
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
    /// radius is divided by the cover scale.
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

        picture.Effect = new BlurEffect { Radius = radius };
    }

    private static readonly ConditionalWeakTable<IImage, IImage> BlurredFills = new();

    /// <summary>
    /// The picture, already blurred, for the bars of an image that fills them
    /// with a copy of itself: drawn once at 480×270 (a blur hides the
    /// resolution) and shown stretched like any other picture. It is drawn
    /// 1.4 times bigger than the frame and centred, so the faded edge a blur
    /// leaves falls outside it.
    /// </summary>
    public static IImage? BlurredFill(IImage? source)
    {
        if (source is null)
            return null;

        if (BlurredFills.TryGetValue(source, out var cached))
            return cached;

        const int Width = 480, Height = 270;
        const double Oversize = 1.4;

        var targetW = Width * Oversize;
        var targetH = Height * Oversize;
        var scale = Math.Max(targetW / Math.Max(1, source.Size.Width), targetH / Math.Max(1, source.Size.Height));
        var drawnW = source.Size.Width * scale;
        var drawnH = source.Size.Height * scale;

        var frame = new Canvas { Width = Width, Height = Height, ClipToBounds = true, Background = Brushes.Black };
        var picture = new Image
        {
            Source = source,
            Stretch = Stretch.Fill,
            Width = drawnW,
            Height = drawnH,
            // 10 here is 40 on a 1920-wide screen.
            Effect = new BlurEffect { Radius = 10 },
        };
        Canvas.SetLeft(picture, (Width - drawnW) / 2);
        Canvas.SetTop(picture, (Height - drawnH) / 2);
        frame.Children.Add(picture);
        frame.Measure(new Size(Width, Height));
        frame.Arrange(new Rect(0, 0, Width, Height));

        try
        {
            var blurred = new RenderTargetBitmap(new PixelSize(Width, Height));
            blurred.Render(frame);
            BlurredFills.AddOrUpdate(source, blurred);
            return blurred;
        }
        catch
        {
            // No drawing here (no screen): the picture as it is.
            return null;
        }
    }

    /// <summary>True when the picture leaves part of the output uncovered.</summary>
    public static bool NeedsFill(MediaItem? media) => VideoBackdrop.NeedsFill(media);

    public static IBrush FillBrush(MediaItem? media)
    {
        var hex = media?.FillColor;
        return !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Black;
    }

    private static void Clear(Image picture)
    {
        picture.Width = double.NaN;
        picture.Height = double.NaN;
        picture.HorizontalAlignment = HorizontalAlignment.Stretch;
        picture.VerticalAlignment = VerticalAlignment.Stretch;
        picture.RenderTransform = null;
        picture.Clip = null;
    }
}
