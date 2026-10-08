using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class ImageDecodeSizeTests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(1.0005)]
    public void Without_zoom_the_file_is_not_even_measured(double zoom)
    {
        Assert.False(ImageDecodeSize.NeedsFileSize(zoom));
        Assert.Equal(ImageDecodeSize.Baseline,
            ImageDecodeSize.For(MediaScaling.Fill, zoom, null, null, 5000, 3000));
    }

    [Fact]
    public void A_zoomed_landscape_picture_is_read_at_screen_width_times_zoom()
    {
        Assert.Equal(3072, ImageDecodeSize.For(MediaScaling.Fill, 1.6, null, null, 3840, 2160));
    }

    [Fact]
    public void It_never_goes_past_the_size_of_the_file()
    {
        Assert.Equal(2000, ImageDecodeSize.For(MediaScaling.Fill, 3, null, null, 2000, 1125));
    }

    [Fact]
    public void It_never_goes_past_4096()
    {
        Assert.Equal(4096, ImageDecodeSize.For(MediaScaling.Fill, 4, null, null, 8000, 4500));
    }

    [Fact]
    public void A_portrait_page_shown_whole_needs_only_the_width_it_shows_at()
    {
        // 2715x3840 fitted to 1080 tall is 764 wide; at 160 % that is 1222.
        var width = ImageDecodeSize.For(MediaScaling.Fit, 1.6, null, null, 2715, 3840);
        Assert.InRange(width, 1200, 1230);
    }

    [Fact]
    public void A_portrait_page_that_fills_the_screen_is_as_wide_as_the_screen()
    {
        Assert.Equal(2715, ImageDecodeSize.For(MediaScaling.Fill, 2, null, null, 2715, 3840));
        Assert.Equal(1920 * 1.2, ImageDecodeSize.For(MediaScaling.Fill, 1.2, null, null, 2715, 3840), 1);
    }

    [Fact]
    public void A_panorama_that_fills_the_screen_is_wider_than_the_screen()
    {
        // 3:1 covering 1080 tall is 3240 wide before the zoom.
        Assert.Equal(3888, ImageDecodeSize.For(MediaScaling.Fill, 1.2, null, null, 6000, 2000));
    }

    [Fact]
    public void A_fixed_size_makes_the_box_smaller()
    {
        // A square 1080x1080 box with a wide picture fitted inside: 1080 wide, at 2x = 2160.
        Assert.Equal(2160, ImageDecodeSize.For(MediaScaling.Fit, 2, 1080, 1080, 4000, 2250));
    }

    [Fact]
    public void A_stretched_picture_is_as_wide_as_the_box()
    {
        Assert.Equal(3840, ImageDecodeSize.For(MediaScaling.Stretch, 2, null, null, 6000, 1000));
    }

    [Fact]
    public void An_unknown_file_size_keeps_the_baseline()
    {
        Assert.Equal(ImageDecodeSize.Baseline, ImageDecodeSize.For(MediaScaling.Fill, 2, null, null, 0, 0));
    }
}
