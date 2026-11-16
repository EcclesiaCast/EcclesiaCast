using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class VideoBackdropTests
{
    private static MediaItem Fitted() => new()
    {
        Type = MediaType.Video,
        Scaling = MediaScaling.Fit,
        FillMediaId = MediaItem.FillWithSelfId,
    };

    [Fact]
    public void A_video_the_shape_of_the_screen_shows_no_bars()
    {
        Assert.False(VideoBackdrop.ShowsBars(Fitted(), 1920, 1080, 1920, 1080));
        Assert.False(VideoBackdrop.ShowsBars(Fitted(), 1280, 720, 1920, 1080));
        Assert.False(VideoBackdrop.ShowsBars(Fitted(), 3840, 2160, 1366, 768));
    }

    [Fact]
    public void Even_a_thin_gap_is_filled()
    {
        // 1920×1088 (a decoder rounding up) leaves a line of 4 pixels above and
        // below: black lines there would be noticed.
        Assert.True(VideoBackdrop.ShowsBars(Fitted(), 1920, 1088, 1920, 1080));
    }

    [Theory]
    [InlineData(1440, 1080, 1920, 1080)] // 4:3 on a 16:9 screen
    [InlineData(1080, 1920, 1920, 1080)] // vertical
    [InlineData(1080, 1080, 1920, 1080)] // square
    [InlineData(1920, 1080, 1024, 768)]  // 16:9 on Carlos's projector
    public void A_video_of_another_shape_shows_bars(double sw, double sh, double screenW, double screenH) =>
        Assert.True(VideoBackdrop.ShowsBars(Fitted(), sw, sh, screenW, screenH));

    [Fact]
    public void A_4_3_video_on_a_4_3_projector_shows_no_bars() =>
        Assert.False(VideoBackdrop.ShowsBars(Fitted(), 1024, 768, 1024, 768));

    [Fact]
    public void Fill_and_stretch_never_show_bars()
    {
        var video = Fitted();
        video.Scaling = MediaScaling.Fill;
        Assert.False(VideoBackdrop.ShowsBars(video, 1080, 1920, 1920, 1080));
        video.Scaling = MediaScaling.Stretch;
        Assert.False(VideoBackdrop.ShowsBars(video, 1080, 1920, 1920, 1080));
    }

    [Fact]
    public void A_zoom_out_or_a_fixed_size_shows_bars_whatever_the_shape()
    {
        var zoomed = Fitted();
        zoomed.Zoom = 0.8;
        Assert.True(VideoBackdrop.ShowsBars(zoomed, 1920, 1080, 1920, 1080));

        var framed = Fitted();
        framed.Scaling = MediaScaling.Fill;
        framed.FrameWidth = 1400;
        framed.FrameHeight = 1080;
        Assert.True(VideoBackdrop.ShowsBars(framed, 1920, 1080, 1920, 1080));
    }

    [Fact]
    public void Before_the_size_is_known_no_bars_are_drawn() =>
        Assert.False(VideoBackdrop.ShowsBars(Fitted(), 0, 0, 1920, 1080));

    [Theory]
    [InlineData(1920, 1080, 128, 72)]
    [InlineData(1080, 1920, 72, 128)]
    [InlineData(1440, 1080, 96, 72)]
    [InlineData(64, 36, 64, 36)] // smaller than the small picture: left as it is
    public void The_small_picture_keeps_the_shape(int w, int h, int sw, int sh) =>
        Assert.Equal((sw, sh), VideoBackdrop.SmallSize(w, h));

    [Fact]
    public void Shrinking_a_flat_frame_keeps_its_colour()
    {
        const int w = 160, h = 90, stride = w * 4 + 64;
        var frame = new byte[stride * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var at = y * stride + x * 4;
                frame[at] = 30;      // blue
                frame[at + 1] = 120; // green
                frame[at + 2] = 200; // red
            }

        var small = new uint[16 * 9];
        VideoBackdrop.Shrink(frame, w, h, stride, small, 16, 9);

        Assert.All(small, p => Assert.Equal(0x00C8781Eu, p));
    }

    [Fact]
    public void Softening_spreads_a_bright_dot_and_darkens_it()
    {
        const int w = 9, h = 9;
        var pixels = new uint[w * h];
        pixels[4 * w + 4] = 0x00FFFFFF;

        VideoBackdrop.Soften(pixels, w, h, new uint[w * h]);

        var centre = pixels[4 * w + 4] & 0xFF;
        var near = pixels[4 * w + 5] & 0xFF;
        var corner = pixels[0] & 0xFF;
        Assert.True(centre > 0 && centre < 255);
        Assert.True(near > 0 && near <= centre);
        Assert.Equal(0u, corner);
        Assert.All(pixels, p => Assert.Equal(0xFF000000u, p & 0xFF000000));
    }

    [Fact]
    public void Softening_a_flat_picture_only_darkens_it()
    {
        var pixels = Enumerable.Repeat(0x00646464u, 12).ToArray();
        VideoBackdrop.Soften(pixels, 4, 3, new uint[12]);
        // 100 × 0.8 = 80.
        Assert.All(pixels, p => Assert.Equal(0xFF505050u, p));
    }
}
