using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Core.Tests.Displays;

public class FloatingOutputTests
{
    [Fact]
    public void The_entry_is_told_apart_from_real_screens_and_from_the_stream()
    {
        var entry = FloatingOutput.For(OutputShape.Wide);

        Assert.True(FloatingOutput.IsFloating(entry));
        Assert.False(BroadcastOnlyOutput.IsBroadcastOnly(entry));
        Assert.False(FloatingOutput.IsFloating(new DisplayInfo(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true)));
        Assert.False(FloatingOutput.IsFloating(null));
    }

    [Fact]
    public void The_entry_has_the_proportion_of_the_shape()
    {
        var wide = FloatingOutput.For(OutputShape.Wide);
        var classic = FloatingOutput.For(OutputShape.Classic);

        Assert.Equal(16d / 9d, AspectFit.RatioOf(wide.Width, wide.Height), 3);
        Assert.Equal(4d / 3d, AspectFit.RatioOf(classic.Width, classic.Height), 3);
    }

    [Fact]
    public void Quick_sizes_are_the_ones_promised_and_keep_the_shape()
    {
        Assert.Equal(["640×360", "1280×720", "1920×1080"], FloatingOutput.QuickSizes(OutputShape.Wide).Select(s => s.ToString()));
        Assert.Equal(["640×480", "1024×768", "1440×1080"], FloatingOutput.QuickSizes(OutputShape.Classic).Select(s => s.ToString()));

        foreach (var shape in new[] { OutputShape.Wide, OutputShape.Classic })
            foreach (var size in FloatingOutput.QuickSizes(shape))
                Assert.Equal(size, FloatingOutput.WithWidth(shape, size.Width));
    }

    [Fact]
    public void A_width_too_small_to_read_goes_up_to_the_minimum()
    {
        Assert.Equal(new PictureSize(FloatingOutput.MinWidth, 135), FloatingOutput.WithWidth(OutputShape.Wide, 10));
    }

    [Theory]
    [InlineData(1920, 1080, 16d / 9d, 0, 0, 1920, 1080)]   // same shape: all of it
    [InlineData(1920, 1080, 4d / 3d, 240, 0, 1440, 1080)]  // 4:3 maximized on a wide screen: bands at the sides
    [InlineData(800, 800, 16d / 9d, 0, 175, 800, 450)]     // a square window: bands above and below
    public void Another_shape_is_centred_with_bands_never_stretched(
        double width, double height, double ratio, double x, double y, double w, double h)
    {
        var fit = FloatingOutput.Fit(width, height, ratio);

        Assert.Equal((x, y, w, h), (Math.Round(fit.X), Math.Round(fit.Y), Math.Round(fit.Width), Math.Round(fit.Height)));
    }

    [Fact]
    public void Dragging_the_right_edge_makes_the_inside_keep_the_shape()
    {
        // A border of 16 across and 39 down (sides plus the title bar).
        var proposed = new ScreenRect(100, 100, 100 + 1296, 100 + 400);

        var kept = FloatingOutput.KeepShape(FloatingOutput.Edge.Right, proposed, 16, 39, 16d / 9d);

        Assert.Equal(1280, kept.Width - 16);
        Assert.Equal(720, kept.Height - 39);
        Assert.Equal((100, 100), (kept.Left, kept.Top));
    }

    [Fact]
    public void Dragging_the_bottom_edge_moves_the_width()
    {
        var proposed = new ScreenRect(0, 0, 500, 39 + 480);

        var kept = FloatingOutput.KeepShape(FloatingOutput.Edge.Bottom, proposed, 16, 39, 4d / 3d);

        Assert.Equal(640, kept.Width - 16);
        Assert.Equal(480, kept.Height - 39);
    }

    [Fact]
    public void Dragging_a_top_corner_keeps_the_bottom_where_it_was()
    {
        var proposed = new ScreenRect(200, 50, 200 + 16 + 640, 900);

        var kept = FloatingOutput.KeepShape(FloatingOutput.Edge.TopLeft, proposed, 16, 39, 16d / 9d);

        Assert.Equal(900, kept.Bottom);
        Assert.Equal(360, kept.Height - 39);
    }

    [Fact]
    public void A_window_on_a_screen_stays_where_it_was()
    {
        var screens = new[] { new ScreenRect(0, 0, 1920, 1080), new ScreenRect(1920, 0, 3200, 1024) };

        Assert.Equal((2000, 100), FloatingOutput.Rescue(new ScreenRect(2000, 100, 2640, 460), screens, screens[0]));
        // Half out of the screen, but the title bar can still be grabbed.
        Assert.Equal((-300, 500), FloatingOutput.Rescue(new ScreenRect(-300, 500, 340, 860), screens, screens[0]));
    }

    [Fact]
    public void A_window_left_on_a_screen_that_is_gone_comes_back_to_the_middle_of_the_main_one()
    {
        var screens = new[] { new ScreenRect(0, 0, 1920, 1080) };

        Assert.Equal((640, 360), FloatingOutput.Rescue(new ScreenRect(2500, 100, 3140, 460), screens, screens[0]));
        // Only the bottom showing: the title bar is above the screen, out of reach.
        Assert.Equal((640, 360), FloatingOutput.Rescue(new ScreenRect(640, -350, 1280, 10), screens, screens[0]));
    }

    [Fact]
    public void The_layout_is_remembered_and_nonsense_falls_back_to_the_default()
    {
        var layout = new FloatingOutput.Layout(OutputShape.Classic, 1024, 120.5, -40, true);

        Assert.Equal(layout, FloatingOutput.Layout.Parse(layout.ToSetting()));
        Assert.Equal(FloatingOutput.Layout.Default, FloatingOutput.Layout.Parse(null));
        Assert.Equal(FloatingOutput.Layout.Default, FloatingOutput.Layout.Parse("basura"));

        var never = FloatingOutput.Layout.Parse(FloatingOutput.Layout.Default.ToSetting());
        Assert.True(double.IsNaN(never.Left));
        Assert.Equal(new PictureSize(1280, 720), never.Picture);
    }
}
