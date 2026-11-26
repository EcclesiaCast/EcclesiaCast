using System.Buffers.Binary;
using EcclesiaCast.Presentation.Rendering;

namespace EcclesiaCast.Presentation.Tests;

/// <summary>The video's numbers that WPF and Avalonia share.</summary>
public class VideoFrameMathTests
{
    [Theory]
    [InlineData(1920, 1080, 4, 480, 270)]
    [InlineData(1920, 1280, 4, 480, 320)]
    [InlineData(3840, 2160, 8, 480, 270)]
    [InlineData(1280, 720, 3, 426, 240)]
    [InlineData(320, 240, 1, 320, 240)]
    public void The_small_copy_lands_near_480_wide(int width, int height, int step, int smallWidth, int smallHeight)
    {
        Assert.Equal((step, smallWidth, smallHeight), VideoPreviewFrame.SizeFor(width, height));
    }

    [Fact]
    public void Each_small_pixel_is_the_average_of_a_two_by_two_block()
    {
        const int width = 16, height = 8, stride = 128; // padded rows, as VLC asks
        var frame = new byte[stride * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(y * stride + x * 4),
                    (uint)((x * 16) | (y * 32) << 8 | (x + y) * 8 << 16));

        var small = new uint[4 * 2];
        VideoPreviewFrame.Shrink(frame, stride, 4, small, 4, 2);

        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 4; x++)
                Assert.Equal(OldShrink(frame, stride, 4, x, y) | 0xFF000000, small[y * 4 + x]);
    }

    [Fact]
    public void A_small_frame_is_copied_pixel_for_pixel_and_opaque()
    {
        var frame = new byte[2 * 4];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0), 0x00123456);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), 0x00ABCDEF);

        var small = new uint[2];
        VideoPreviewFrame.Shrink(frame, 8, 1, small, 2, 1);

        Assert.Equal([0xFF123456, 0xFFABCDEF], small);
    }

    [Fact]
    public void A_held_picture_waits_two_seconds_or_the_fade_and_one_more()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), VideoHold.Timeout(TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromSeconds(2), VideoHold.Timeout(TimeSpan.FromMilliseconds(320)));
        Assert.Equal(TimeSpan.FromSeconds(4), VideoHold.Timeout(TimeSpan.FromSeconds(3)));
    }

    /// <summary>What the WPF surface did before the count moved here (pointer for pointer).</summary>
    private static uint OldShrink(byte[] frame, int stride, int step, int x, int y)
    {
        var at = y * step * stride + x * step * 4;
        uint Read(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(offset));
        var a = Read(at);
        var b = Read(at + 4);
        var c = Read(at + stride);
        var d = Read(at + stride + 4);
        var lo = ((a & 0x00FF00FF) + (b & 0x00FF00FF) + (c & 0x00FF00FF) + (d & 0x00FF00FF)) >> 2;
        var mid = ((a & 0x0000FF00) + (b & 0x0000FF00) + (c & 0x0000FF00) + (d & 0x0000FF00)) >> 2;
        return (lo & 0x00FF00FF) | (mid & 0x0000FF00);
    }
}
