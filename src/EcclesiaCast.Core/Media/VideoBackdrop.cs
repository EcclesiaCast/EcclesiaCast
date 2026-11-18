namespace EcclesiaCast.Core.Media;

/// <summary>
/// The blurred copy of a video that fills the bars around it, made from the
/// frames the video already decodes: no second player, no second decoding.
///
/// Each frame is shrunk to a picture about 128×72, softened here with a few
/// passes of a box blur and drawn stretched over the whole screen. Shrinking
/// and blurring 9 000 pixels costs a few hundredths of a millisecond against
/// the several milliseconds the full frame takes to reach the screen, and a
/// picture that small, stretched, is blurred by the stretching itself. A blur
/// effect over the full-size video was the other way, and it costs the card a
/// full-screen pass with a wide kernel on every frame.
/// </summary>
public static class VideoBackdrop
{
    /// <summary>The short side of the small picture, in pixels.</summary>
    public const int ShortSide = 72;

    /// <summary>Box blur radius over the small picture, in its own pixels.</summary>
    public const int BlurRadius = 1;

    /// <summary>Passes of the box blur: three are close to a gaussian.</summary>
    public const int BlurPasses = 3;

    /// <summary>
    /// How much of its brightness the blurred copy keeps: a little darker, so
    /// the eye stays on the real video in the middle.
    /// </summary>
    public const double Brightness = 0.8;

    /// <summary>
    /// Shapes this close are the same shape (1366×768 against 16:9). Anything
    /// more leaves a line a few pixels thick that shows if left black.
    /// </summary>
    public const double Tolerance = 0.002;

    /// <summary>
    /// True when the media, as framed, leaves part of the screen uncovered
    /// before even knowing its shape: «Ajustar», a zoom below 1, a shift or a
    /// fixed size. The projector, the previews and the cards paint the fill
    /// only then.
    /// </summary>
    public static bool NeedsFill(MediaItem? media) =>
        media is not null
        && (media.HasFrame
            || media.Zoom is > 0 and < 1
            || media.Scaling == MediaScaling.Fit
            || media.OffsetX != 0
            || media.OffsetY != 0);

    /// <summary>
    /// True when a picture of <paramref name="sourceWidth"/> × <paramref name="sourceHeight"/>
    /// really leaves bars on a screen of <paramref name="screenWidth"/> × <paramref name="screenHeight"/>.
    /// A video already the shape of the screen, «Ajustado», covers it whole:
    /// then nothing is drawn behind it and it costs nothing.
    /// </summary>
    public static bool ShowsBars(MediaItem? media, double sourceWidth, double sourceHeight, double screenWidth, double screenHeight)
    {
        if (!NeedsFill(media))
            return false;

        // A zoom out, a shift or a fixed size: the gap is there whatever the shape.
        if (media!.Scaling != MediaScaling.Fit || media.HasFrame || media.Zoom is > 0 and < 1
            || media.OffsetX != 0 || media.OffsetY != 0)
            return true;

        if (sourceWidth <= 0 || sourceHeight <= 0 || screenWidth <= 0 || screenHeight <= 0)
            return false;

        var source = sourceWidth / sourceHeight;
        var screen = screenWidth / screenHeight;
        return Math.Abs(source - screen) / screen > Tolerance;
    }

    /// <summary>The size of the small picture for a frame of this size, keeping its shape.</summary>
    public static (int Width, int Height) SmallSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return (0, 0);

        var step = Math.Max(1.0, Math.Min(width, height) / (double)ShortSide);
        return (Math.Max(1, (int)Math.Round(width / step)), Math.Max(1, (int)Math.Round(height / step)));
    }

    /// <summary>
    /// Shrinks a frame (32-bit pixels, blue first, rows of <paramref name="stride"/>
    /// bytes) into <paramref name="small"/>: each small pixel is the average of
    /// four samples spread over its cell, so fine detail does not flicker.
    /// </summary>
    public static void Shrink(ReadOnlySpan<byte> frame, int width, int height, int stride,
        Span<uint> small, int smallWidth, int smallHeight)
    {
        if (width <= 0 || height <= 0 || smallWidth <= 0 || smallHeight <= 0)
            return;

        var cellW = width / (double)smallWidth;
        var cellH = height / (double)smallHeight;

        // Where the two samples of each column fall, worked out once per frame.
        Span<int> columns = smallWidth <= 512 ? stackalloc int[smallWidth * 2] : new int[smallWidth * 2];
        for (var x = 0; x < smallWidth; x++)
        {
            columns[2 * x] = Math.Min(width - 1, (int)((x + 0.25) * cellW)) * 4;
            columns[2 * x + 1] = Math.Min(width - 1, (int)((x + 0.75) * cellW)) * 4;
        }

        for (var y = 0; y < smallHeight; y++)
        {
            var y0 = Math.Min(height - 1, (int)((y + 0.25) * cellH));
            var y1 = Math.Min(height - 1, (int)((y + 0.75) * cellH));
            var row0 = frame.Slice(y0 * stride);
            var row1 = frame.Slice(y1 * stride);
            var line = small.Slice(y * smallWidth, smallWidth);
            for (var x = 0; x < line.Length; x++)
            {
                var x0 = columns[2 * x];
                var x1 = columns[2 * x + 1];
                var a = Read(row0, x0);
                var b = Read(row0, x1);
                var c = Read(row1, x0);
                var d = Read(row1, x1);

                // Channel by channel (blue and red together, then green).
                var lo = ((a & 0x00FF00FF) + (b & 0x00FF00FF) + (c & 0x00FF00FF) + (d & 0x00FF00FF)) >> 2;
                var mid = ((a & 0x0000FF00) + (b & 0x0000FF00) + (c & 0x0000FF00) + (d & 0x0000FF00)) >> 2;
                line[x] = (lo & 0x00FF00FF) | (mid & 0x0000FF00);
            }
        }
    }

    /// <summary>One pixel, without its fourth byte (VLC leaves it undefined).</summary>
    private static uint Read(ReadOnlySpan<byte> row, int at) =>
        System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(row.Slice(at, 4)) & 0x00FFFFFF;

    /// <summary>
    /// Softens the small picture in place (box blur, <see cref="BlurPasses"/>
    /// passes across and down, edges repeated) and darkens it to
    /// <see cref="Brightness"/>. Leaves the pixels opaque.
    /// </summary>
    public static void Soften(Span<uint> pixels, int width, int height, Span<uint> scratch)
    {
        if (width <= 0 || height <= 0 || pixels.Length < width * height || scratch.Length < width * height)
            return;

        for (var pass = 0; pass < BlurPasses; pass++)
        {
            BoxPass(pixels, scratch, width, height, 1, width, BlurRadius);
            BoxPass(scratch, pixels, height, width, width, 1, BlurRadius);
        }

        var keep = (uint)Math.Round(Brightness * 256);
        for (var i = 0; i < width * height; i++)
        {
            var p = pixels[i];
            var lo = ((p & 0x00FF00FF) * keep >> 8) & 0x00FF00FF;
            var mid = ((p & 0x0000FF00) * keep >> 8) & 0x0000FF00;
            pixels[i] = 0xFF000000 | lo | mid;
        }
    }

    /// <summary>
    /// One box blur along lines: <paramref name="lines"/> lines of
    /// <paramref name="length"/> pixels, <paramref name="step"/> apart inside a
    /// line and <paramref name="lineStep"/> apart between lines.
    /// </summary>
    private static void BoxPass(Span<uint> from, Span<uint> to, int length, int lines, int step, int lineStep, int radius)
    {
        // Dividing by the window is a multiply and a shift: exact for sums up to 255 × the window.
        var count = 2 * radius + 1;
        var inverse = (1 << 16) / count + 1;
        for (var line = 0; line < lines; line++)
        {
            var start = line * lineStep;
            int b = 0, g = 0, r = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var p = from[start + Math.Clamp(k, 0, length - 1) * step];
                b += (int)(p & 0xFF);
                g += (int)(p >> 8 & 0xFF);
                r += (int)(p >> 16 & 0xFF);
            }

            for (var i = 0; i < length; i++)
            {
                to[start + i * step] = (uint)(b * inverse >> 16) | (uint)(g * inverse >> 16) << 8 | (uint)(r * inverse >> 16) << 16;

                var outgoing = from[start + Math.Max(i - radius, 0) * step];
                var incoming = from[start + Math.Min(i + radius + 1, length - 1) * step];
                b += (int)(incoming & 0xFF) - (int)(outgoing & 0xFF);
                g += (int)(incoming >> 8 & 0xFF) - (int)(outgoing >> 8 & 0xFF);
                r += (int)(incoming >> 16 & 0xFF) - (int)(outgoing >> 16 & 0xFF);
            }
        }
    }
}
