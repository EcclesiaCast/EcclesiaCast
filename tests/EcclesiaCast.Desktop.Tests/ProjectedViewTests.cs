using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Desktop.Controls;
using EcclesiaCast.Desktop.Platform;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Rendering;
using MediaType = EcclesiaCast.Core.Media.MediaType;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>The whole projected picture: the fade between slides, the framing and the fill, the video on the output.</summary>
public sealed class ProjectedViewTests
{
    [AvaloniaFact]
    public void The_fade_between_two_slides_ends_showing_only_the_new_one()
    {
        var first = RenderCases.Load("cancion-4-renglones");
        var second = RenderCases.Load("biblia-con-cita");
        var theme = first.Themes.Single(t => t.Key == "cancion");

        // What the new slide alone looks like, with nothing before it.
        var alone = new ProjectedView();
        byte[] expected;
        using (var drawing = new SlideDrawing(alone))
        {
            alone.Slide = second.BuildSlide(theme);
            expected = Pixels(drawing.Capture());
        }

        var view = new ProjectedView { AnimateTransitions = true };
        using var fading = new SlideDrawing(view);
        view.Slide = first.BuildSlide(theme);
        fading.Settle();
        view.Slide = second.BuildSlide(theme);

        // Halfway, the copy of the old words is still on its way out.
        fading.Settle();
        Assert.True(TextCopies(view) > 0, "La letra anterior no se fue con un fundido.");

        // The fade takes 280 ms; the clock of the animations is the real one.
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromMilliseconds(700))
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(10);
        }

        Assert.Equal(0, TextCopies(view));
        var difference = Difference(expected, Pixels(fading.Capture()));
        Assert.True(difference < 0.001, $"Después del fundido queda algo de la diapositiva anterior (diferencia {difference:P2}).");
    }

    [AvaloniaTheory]
    [InlineData(0.5, 0.1, -0.05, false)]
    [InlineData(1.6, -0.2, 0.15, false)]
    [InlineData(0.8, 0.05, 0.05, true)]
    public void A_filled_picture_with_zoom_and_shift_lands_where_fill_framing_says(
        double zoom, double offsetX, double offsetY, bool fixedSize)
    {
        // A red picture of 400×200 on a blue fill: the picture is wider than
        // the screen once it covers it, so «Rellenar» has something to crop.
        var path = RedPicture(400, 200);
        var media = new MediaItem
        {
            Id = 7,
            Type = MediaType.Image,
            Path = path,
            Scaling = MediaScaling.Fill,
            Zoom = zoom,
            OffsetX = offsetX,
            OffsetY = offsetY,
            FillColor = "#0000FF",
            FrameWidth = fixedSize ? 1440 : null,
            FrameHeight = fixedSize ? 810 : null,
        };

        var view = new ProjectedView();
        using var drawing = new SlideDrawing(view);
        view.BackgroundMedia = media;
        WaitFor(() => view.BackgroundImage.IsVisible, drawing);
        drawing.Save(RenderCases.OutputFolder("avalonia-tanda-10"), $"encuadre-{zoom}-{offsetX}-{offsetY}-{fixedSize}");
        var frame = Pixels(drawing.Capture());

        // Where the shared numbers put it.
        const double W = SlideDrawing.Width, H = SlideDrawing.Height;
        var (boxW, boxH) = FillFraming.BoxSize(W, H, media.FrameWidth, media.FrameHeight);
        var (shiftX, shiftY) = FramingNumbers.Offset(media, W, H);
        var place = FillFraming.Place(400, 200, W, H, boxW, boxH, media.HasFrame, FramingNumbers.Zoom(media), shiftX, shiftY);
        var fit = Math.Min(boxW / 400, boxH / 200);
        var width = 400 * fit * place.Scale;
        var height = 200 * fit * place.Scale;
        var picture = new Rect(W / 2 - width / 2 + place.ShiftX, H / 2 - height / 2 + place.ShiftY, width, height);

        // With a fixed size, only the box shows the picture.
        var shown = media.HasFrame
            ? picture.Intersect(new Rect((W - boxW) / 2, (H - boxH) / 2, boxW, boxH))
            : picture;
        shown = shown.Intersect(new Rect(0, 0, W, H));

        var inside = shown.Deflate(4);
        var outside = shown.Inflate(4);
        Assert.True(inside.Width > 50 && inside.Height > 50, "La imagen quedó fuera de la pantalla en el caso de prueba.");

        foreach (var (x, y) in Samples(inside))
            Assert.True(IsRed(frame, x, y), $"En ({x}, {y}) tendría que estar la imagen.");

        foreach (var (x, y) in Border(outside))
        {
            if (x < 0 || y < 0 || x >= W || y >= H)
                continue;
            Assert.True(IsBlue(frame, x, y), $"En ({x}, {y}) tendría que estar el relleno.");
        }
    }

    [AvaloniaFact]
    public void A_card_frames_the_picture_as_the_projector_does()
    {
        var media = new MediaItem
        {
            Id = 8,
            Type = MediaType.Image,
            Path = RedPicture(400, 200),
            Scaling = MediaScaling.Fill,
            Zoom = 0.5,
            OffsetX = 0.1,
            OffsetY = -0.05,
            FillColor = "#0000FF",
        };

        // The projector at full size, and a card a quarter of it: the same
        // picture, scaled down.
        var view = new ProjectedView();
        byte[] screen;
        using (var drawing = new SlideDrawing(view))
        {
            view.BackgroundMedia = media;
            WaitFor(() => view.BackgroundImage.IsVisible, drawing);
            screen = Pixels(drawing.Capture());
        }

        var card = new FramedMediaPreview
        {
            Width = SlideDrawing.Width / 4.0,
            Height = SlideDrawing.Height / 4.0,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Media = media,
        };
        using var cardDrawing = new SlideDrawing(card);
        var small = Pixels(cardDrawing.Capture());

        var checkedPoints = 0;
        for (var y = 8; y < SlideDrawing.Height / 4 - 8; y += 16)
            for (var x = 8; x < SlideDrawing.Width / 4 - 8; x += 16)
            {
                var big = (y * 4, x * 4);
                // Points right on the edge of the picture can go either way.
                if (IsRed(screen, big.Item2, big.Item1) == IsRed(screen, big.Item2 + 8, big.Item1 + 8)
                    && IsRed(screen, big.Item2, big.Item1) == IsRed(screen, big.Item2 - 8, big.Item1 - 8))
                {
                    Assert.Equal(IsRed(screen, big.Item2, big.Item1), IsRed(small, x, y));
                    checkedPoints++;
                }
            }

        Assert.True(checkedPoints > 400);
    }

    [AvaloniaFact]
    public void On_the_output_a_video_background_moves_under_the_slide()
    {
        if (VideoEngineForTests.Engine is not { } engine)
        {
            Assert.Skip("LibVLC no se pudo cargar en esta máquina: la prueba del video con VLC de verdad no corre acá.");
            return;
        }

        var before = PlatformServices.Video;
        PlatformServices.Video = new VideoEngine(engine);
        var view = new ProjectedView { IsLiveOutput = true };
        var drawing = new SlideDrawing(view);
        try
        {
            view.BackgroundMedia = new MediaItem
            {
                Id = 3,
                Type = MediaType.Video,
                Path = VideoEngineForTests.TestVideo,
                Muted = true,
            };

            WaitFor(() => view.Video!.FramesDrawn > 3, drawing, TimeSpan.FromSeconds(20));
            var frame = Pixels(drawing.Capture());

            // The test pattern is bright and colourful: the middle of the
            // screen is far from the black behind everything.
            Assert.True(Brightness(frame) > 40, "El video de fondo no se ve en la salida.");
            Assert.True(view.PlaybackState.HasVideo);
        }
        finally
        {
            view.StopVideos();
            drawing.Dispose();
            Dispatcher.UIThread.RunJobs();
            PlatformServices.Video = before;
        }
    }

    // ── Ayudantes ────────────────────────────────────────────────

    /// <summary>The copies of the old words that SlideView fades out, still on screen.</summary>
    private static int TextCopies(ProjectedView view) =>
        view.SlideView.GetVisualDescendants().OfType<Image>().Count(image => string.IsNullOrEmpty(image.Name));

    private static void WaitFor(Func<bool> condition, SlideDrawing drawing, TimeSpan? limit = null)
    {
        var clock = Stopwatch.StartNew();
        while (!condition() && clock.Elapsed < (limit ?? TimeSpan.FromSeconds(10)))
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }
        Assert.True(condition(), "No llegó a tiempo.");
        drawing.Settle();
    }

    private static string RedPicture(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ecclesiacast-prueba-rojo-{width}x{height}.png");
        using var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using (var pixels = bitmap.Lock())
        {
            unsafe
            {
                for (var y = 0; y < height; y++)
                {
                    var row = (uint*)((byte*)pixels.Address + (long)y * pixels.RowBytes);
                    for (var x = 0; x < width; x++)
                        row[x] = 0xFFFF0000;
                }
            }
        }
        bitmap.Save(path, PngBitmapEncoderOptions.Default);
        return path;
    }

    private static IEnumerable<(int X, int Y)> Samples(Rect area)
    {
        for (var i = 0; i <= 8; i++)
            for (var j = 0; j <= 8; j++)
                yield return ((int)(area.X + area.Width * i / 8), (int)(area.Y + area.Height * j / 8));
    }

    private static IEnumerable<(int X, int Y)> Border(Rect area)
    {
        for (var i = 0; i <= 16; i++)
        {
            var x = (int)(area.X + area.Width * i / 16);
            var y = (int)(area.Y + area.Height * i / 16);
            yield return (x, (int)area.Y);
            yield return (x, (int)area.Bottom);
            yield return ((int)area.X, y);
            yield return ((int)area.Right, y);
        }
    }

    private static bool IsRed(byte[] frame, int x, int y)
    {
        var at = (y * SlideDrawing.Width + x) * 4;
        return frame[at + 2] > 200 && frame[at + 1] < 40 && frame[at] < 40;
    }

    private static bool IsBlue(byte[] frame, int x, int y)
    {
        var at = (y * SlideDrawing.Width + x) * 4;
        return frame[at] > 200 && frame[at + 1] < 40 && frame[at + 2] < 40;
    }

    /// <summary>The frame as rows of B, G, R, A.</summary>
    private static byte[] Pixels(WriteableBitmap frame)
    {
        using (frame)
        using (var pixels = frame.Lock())
        {
            var width = frame.PixelSize.Width;
            var height = frame.PixelSize.Height;
            var result = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
                System.Runtime.InteropServices.Marshal.Copy(pixels.Address + y * pixels.RowBytes, result, y * width * 4, width * 4);

            // The capture may come red first: turned to blue first.
            if (pixels.Format == PixelFormat.Rgba8888)
                for (var i = 0; i < result.Length; i += 4)
                    (result[i], result[i + 2]) = (result[i + 2], result[i]);
            return result;
        }
    }

    /// <summary>The share of the colour that differs, 0–1.</summary>
    private static double Difference(byte[] a, byte[] b)
    {
        long sum = 0;
        for (var i = 0; i < a.Length; i++)
            if (i % 4 != 3)
                sum += Math.Abs(a[i] - b[i]);
        return sum / (a.Length * 0.75 * 255.0);
    }

    private static double Brightness(byte[] frame)
    {
        long sum = 0;
        var count = 0;
        for (var y = 200; y < 880; y += 40)
            for (var x = 300; x < 1620; x += 40)
            {
                var at = (y * SlideDrawing.Width + x) * 4;
                sum += frame[at] + frame[at + 1] + frame[at + 2];
                count += 3;
            }
        return sum / (double)count;
    }
}
