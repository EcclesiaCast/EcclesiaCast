using System.Buffers.Binary;

namespace EcclesiaCast.Presentation.Rendering;

/// <summary>
/// The small copy of the video for the operator's Live panel. That panel is a
/// box about 300 px wide: handing it the projector's full 1920×1080 picture
/// made the operator's window upload ~8 MB to the graphics card on every frame
/// just to throw most of it away, which is what made a modest PC stutter. It
/// gets its own small copy instead, a few times a second, shrunk on VLC's
/// thread. The video surfaces of WPF and of Avalonia only draw it.
/// </summary>
public static class VideoPreviewFrame
{
    /// <summary>The width the small copy lands near.</summary>
    public const int TargetWidth = 480;

    /// <summary>How many small copies are made per second, at most.</summary>
    public const int PerSecond = 15;

    /// <summary>
    /// The step between the pixels taken (1 when the frame is already small)
    /// and the size of the small copy.
    /// </summary>
    public static (int Step, int Width, int Height) SizeFor(int width, int height)
    {
        var step = Math.Max(1, (int)Math.Round(width / (double)TargetWidth));
        return (step, Math.Max(1, width / step), Math.Max(1, height / step));
    }

    /// <summary>
    /// Copies a frame (32-bit pixels, blue first, rows of <paramref name="stride"/>
    /// bytes) into <paramref name="small"/>, averaging each 2×2 block at every
    /// <paramref name="step"/>. The pixels come out opaque.
    /// </summary>
    public static void Shrink(ReadOnlySpan<byte> frame, int stride, int step,
        Span<uint> small, int smallWidth, int smallHeight)
    {
        // The second sample of each pair; none when the frame is so small it
        // is copied pixel for pixel.
        var dx = step > 1 ? 4 : 0;
        var dy = step > 1 ? stride : 0;

        for (var y = 0; y < smallHeight; y++)
        {
            var row = (long)y * step * stride;
            var line = small.Slice(y * smallWidth, smallWidth);
            for (var x = 0; x < line.Length; x++)
            {
                var at = (int)(row + (long)x * step * 4);
                var a = Read(frame, at);
                var b = Read(frame, at + dx);
                var c = Read(frame, at + dy);
                var d = Read(frame, at + dy + dx);

                // Average the four pixels channel by channel (B, G, R).
                var lo = ((a & 0x00FF00FF) + (b & 0x00FF00FF) + (c & 0x00FF00FF) + (d & 0x00FF00FF)) >> 2;
                var mid = ((a & 0x0000FF00) + (b & 0x0000FF00) + (c & 0x0000FF00) + (d & 0x0000FF00)) >> 2;
                line[x] = 0xFF000000 | (lo & 0x00FF00FF) | (mid & 0x0000FF00);
            }
        }
    }

    private static uint Read(ReadOnlySpan<byte> frame, int at) =>
        BinaryPrimitives.ReadUInt32LittleEndian(frame.Slice(at, 4));
}
