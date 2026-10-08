using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class PosterSizeTests
{
    [Fact]
    public void A_screen_shaped_picture_comes_out_320_by_180_as_it_always_did()
    {
        Assert.Equal((320, 180), PosterSize.For(1920, 1080));
    }

    [Fact]
    public void A_tall_page_keeps_its_shape_with_320_on_the_long_side()
    {
        // A letter page, the manual's cover: 8.5 × 11 inches.
        Assert.Equal((247, 320), PosterSize.For(3300, 4268));
        Assert.Equal((226, 320), PosterSize.For(2480, 3508));
    }

    [Fact]
    public void A_wide_picture_keeps_its_shape_too()
    {
        Assert.Equal((320, 213), PosterSize.For(3000, 2000));
        Assert.Equal((320, 80), PosterSize.For(4000, 1000));
    }

    [Fact]
    public void A_picture_smaller_than_a_poster_is_not_blown_up()
    {
        Assert.Equal((200, 100), PosterSize.For(200, 100));
    }

    [Fact]
    public void A_sliver_still_has_at_least_one_pixel_on_the_short_side()
    {
        Assert.Equal((320, 1), PosterSize.For(10000, 5));
    }

    [Fact]
    public void An_unknown_size_gives_a_square()
    {
        Assert.Equal((320, 320), PosterSize.For(0, 0));
    }
}
