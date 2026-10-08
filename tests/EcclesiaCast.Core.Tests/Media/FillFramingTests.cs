using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class FillFramingTests
{
    private const double W = 1920;
    private const double H = 1080;

    /// <summary>Where the picture ends up on the container once the placement is applied to the whole picture, drawn fitted in the middle of its box.</summary>
    private static FramedRect Final(double sw, double sh, FillFraming.Placement place, double boxW = W, double boxH = H)
    {
        var fit = Math.Min(boxW / sw, boxH / sh);
        double fw = sw * fit, fh = sh * fit;

        // Drawn whole, centred in the container.
        double x = (W - fw) / 2, y = (H - fh) / 2;

        // Scaled around its own middle, then moved.
        double cx = W / 2, cy = H / 2;
        return new FramedRect(
            cx + (x - cx) * place.Scale + place.ShiftX,
            cy + (y - cy) * place.Scale + place.ShiftY,
            fw * place.Scale,
            fh * place.Scale);
    }

    private static FillFraming.Placement Place(
        double sw, double sh, double zoom = 1, double shiftX = 0, double shiftY = 0,
        double boxW = W, double boxH = H, bool frame = false) =>
        FillFraming.Place(sw, sh, W, H, boxW, boxH, frame, zoom, shiftX, shiftY);

    [Fact]
    public void A_picture_the_shape_of_the_screen_is_left_alone()
    {
        var place = Place(3840, 2160);

        Assert.Equal(1, place.CoverScale, 9);
        Assert.Equal(1, place.Scale, 9);
        Assert.Equal(0, place.ShiftX, 9);
        Assert.Equal(0, place.ShiftY, 9);
    }

    [Fact]
    public void A_tall_page_covers_the_screen_from_its_top_left_corner()
    {
        // 600×850 covers 1920 wide, so it is 2720 high; the part shown is the
        // top, as UniformToFill has always shown it.
        var final = Final(600, 850, Place(600, 850));

        Assert.Equal(0, final.X, 6);
        Assert.Equal(0, final.Y, 6);
        Assert.Equal(1920, final.Width, 6);
        Assert.Equal(2720, final.Height, 6);
    }

    [Fact]
    public void A_wide_picture_also_starts_at_the_top_left()
    {
        // 2:1 covers 1080 high, so it is 2160 wide and its right end is cut.
        var final = Final(1200, 600, Place(1200, 600));

        Assert.Equal(0, final.X, 6);
        Assert.Equal(0, final.Y, 6);
        Assert.Equal(2160, final.Width, 6);
        Assert.Equal(1080, final.Height, 6);
    }

    [Fact]
    public void Shifting_a_tall_page_up_brings_its_bottom_to_the_bottom_of_the_screen()
    {
        // The crop used to be fixed: the page could never be moved down to
        // its end. 2720 − 1080 = 1640 pixels is 1.52 screens: more than the
        // old limit of one.
        var final = Final(600, 850, Place(600, 850, shiftY: -1640));

        Assert.Equal(H, final.Bottom, 6);
        Assert.InRange(1640 / H, 1.01, 10);
    }

    [Fact]
    public void Zooming_out_shows_more_of_the_page_around_the_middle_of_the_screen()
    {
        var full = Final(600, 850, Place(600, 850));
        var half = Final(600, 850, Place(600, 850, zoom: 0.5));

        // Half as big, with the screen's middle as the fixed point.
        Assert.Equal(full.Width / 2, half.Width, 6);
        Assert.Equal(W / 2 + (full.X - W / 2) / 2, half.X, 6);
        Assert.Equal(H / 2 + (full.Y - H / 2) / 2, half.Y, 6);
        Assert.True(half.Bottom < full.Bottom);
    }

    [Fact]
    public void Zooming_a_screen_shaped_picture_keeps_it_centred()
    {
        var zoomed = Final(1920, 1080, Place(1920, 1080, zoom: 2));

        Assert.Equal(W / 2, zoomed.X + zoomed.Width / 2, 6);
        Assert.Equal(H / 2, zoomed.Y + zoomed.Height / 2, 6);
    }

    [Fact]
    public void A_shift_moves_the_picture_by_that_much()
    {
        var still = Final(1200, 600, Place(1200, 600, zoom: 1.5));
        var moved = Final(1200, 600, Place(1200, 600, zoom: 1.5, shiftX: 100, shiftY: -40));

        Assert.Equal(still.X + 100, moved.X, 6);
        Assert.Equal(still.Y - 40, moved.Y, 6);
    }

    [Fact]
    public void The_fixed_size_box_stays_put_while_the_picture_moves_and_zooms_behind_it()
    {
        // 1080×1080 box in the middle of the screen, a tall page inside it.
        const double box = 1080;
        var place = Place(600, 850, zoom: 1.5, shiftX: 30, shiftY: -200, boxW: box, boxH: box, frame: true);
        var window = place.Window!.Value;

        // Map the window's corners from the picture's own coordinates to the
        // screen: they must land on the box, whatever the zoom and shift.
        var fit = Math.Min(box / 600, box / 850);
        double fw = 600 * fit, fh = 850 * fit;
        double ToScreenX(double q) => W / 2 + (q - fw / 2) * place.Scale + place.ShiftX;
        double ToScreenY(double q) => H / 2 + (q - fh / 2) * place.Scale + place.ShiftY;

        Assert.Equal((W - box) / 2, ToScreenX(window.X), 6);
        Assert.Equal((W + box) / 2, ToScreenX(window.Right), 6);
        Assert.Equal((H - box) / 2, ToScreenY(window.Y), 6);
        Assert.Equal((H + box) / 2, ToScreenY(window.Bottom), 6);
    }

    [Fact]
    public void Without_a_fixed_size_there_is_no_window()
    {
        Assert.Null(Place(600, 850).Window);
    }

    [Fact]
    public void A_fixed_size_is_scaled_to_the_container_and_never_bigger_than_it()
    {
        Assert.Equal((480.0, 270.0), FillFraming.BoxSize(480, 270, null, null));
        Assert.Equal((240.0, 135.0), FillFraming.BoxSize(480, 270, 960, 540));
        Assert.Equal((480.0, 270.0), FillFraming.BoxSize(480, 270, 4000, 3000));
        Assert.Equal((270.0, 270.0), FillFraming.BoxSize(480, 270, 1080, 1920));
    }

    [Fact]
    public void The_natural_bounds_of_a_tall_page_hang_below_the_screen()
    {
        var bounds = FillFraming.NaturalBounds(600, 850, W, H, W, H);

        Assert.Equal(new FramedRect(0, 0, 1920, 2720), bounds, new RectComparer());
    }

    [Fact]
    public void The_natural_bounds_with_a_fixed_size_start_at_the_box()
    {
        var bounds = FillFraming.NaturalBounds(600, 850, W, H, 1080, 1080);

        Assert.Equal((W - 1080) / 2, bounds.X, 6);
        Assert.Equal(0, bounds.Y, 6);
        Assert.Equal(1080, bounds.Width, 6);
        Assert.Equal(1080 * 850 / 600.0, bounds.Height, 6);
    }

    private sealed class RectComparer : IEqualityComparer<FramedRect>
    {
        public bool Equals(FramedRect a, FramedRect b) =>
            Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(a.Y - b.Y) < 1e-6
            && Math.Abs(a.Width - b.Width) < 1e-6 && Math.Abs(a.Height - b.Height) < 1e-6;

        public int GetHashCode(FramedRect r) => 0;
    }
}
