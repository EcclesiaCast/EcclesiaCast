using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.Core.Tests.Themes;

public class LineBandLayoutTests
{
    private static BandBox Letter(double x, double y, double w = 10, double h = 40) => new(x, y, w, h);

    [Fact]
    public void Letters_on_the_same_height_make_one_line()
    {
        var lines = LineBandLayout.Lines(
        [
            Letter(100, 0), Letter(110, 0), Letter(140, 2),
            Letter(80, 50), Letter(90, 50),
        ]);

        Assert.Equal(2, lines.Count);
        Assert.Equal(100, lines[0].X);
        Assert.Equal(150, lines[0].Right);
        Assert.Equal(80, lines[1].X);
        Assert.Equal(100, lines[1].Right);
    }

    [Fact]
    public void Text_bands_are_a_little_wider_than_their_line()
    {
        var bands = LineBandLayout.Bands([new BandBox(100, 0, 200, 40)], LineBand.Text, fontSize: 50, 0, 1920);

        var band = Assert.Single(bands);
        Assert.Equal(100 - 50 * LineBandLayout.SidePadding, band.X, 6);
        Assert.Equal(200 + 2 * 50 * LineBandLayout.SidePadding, band.Width, 6);
        Assert.True(band.Y > 0 && band.Bottom < 40);
    }

    [Fact]
    public void Full_width_bands_go_from_edge_to_edge()
    {
        var bands = LineBandLayout.Bands(
            [new BandBox(100, 0, 200, 40), new BandBox(150, 45, 100, 40)], LineBand.FullWidth, 50, -300, 1620);

        Assert.All(bands, b =>
        {
            Assert.Equal(-300, b.X);
            Assert.Equal(1920, b.Width);
        });
        Assert.True(bands[0].Bottom < bands[1].Y, "two lines never touch");
    }

    [Fact]
    public void No_bands_when_the_theme_has_none()
    {
        Assert.Empty(LineBandLayout.Bands([new BandBox(0, 0, 10, 10)], LineBand.None, 50, 0, 1920));
    }

    [Fact]
    public void The_band_looks_set_bands_and_the_others_take_them_off()
    {
        var withBands = new SlideTheme { LineBand = LineBand.FullWidth, LineBandColor = "#FFFFFF" };
        foreach (var preset in ThemePresets.All)
        {
            var look = preset.ApplyTo(withBands);
            if (preset.Name.StartsWith("Franja", StringComparison.Ordinal))
            {
                Assert.NotEqual(LineBand.None, look.LineBand);
                Assert.True(look.TransparentBackground);
                // The letter is the opposite of its band, or it does not read.
                Assert.NotEqual(look.LineBandColor, look.TextColor);
            }
            else
            {
                Assert.Equal(LineBand.None, look.LineBand);
            }
        }
    }
}
