using System.Globalization;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class CardZoomTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("grande")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void A_missing_or_unreadable_size_is_the_default(string? saved)
    {
        Assert.Equal(CardZoom.Default, CardZoom.Parse(saved, CardZoom.SlidesMin, CardZoom.SlidesMax));
    }

    [Fact]
    public void A_saved_size_is_read_back_with_a_dot()
    {
        Assert.Equal(1.5, CardZoom.Parse("1.5", CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
    }

    [Fact]
    public void A_size_outside_the_range_is_brought_back_to_its_edge()
    {
        Assert.Equal(CardZoom.SlidesMin, CardZoom.Parse("0.1", CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
        Assert.Equal(CardZoom.SlidesMax, CardZoom.Parse("9", CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
        Assert.Equal(CardZoom.MediaMax, CardZoom.Parse("9", CardZoom.MediaMin, CardZoom.MediaMax), precision: 6);
    }

    [Fact]
    public void The_media_bar_reads_its_own_wider_range()
    {
        Assert.Equal(2.2, CardZoom.Parse("2.2", CardZoom.MediaMin, CardZoom.MediaMax), precision: 6);
    }

    [Fact]
    public void A_size_is_written_with_one_decimal_and_a_dot()
    {
        Assert.Equal("1.0", CardZoom.Format(1.0));
        Assert.Equal("0.6", CardZoom.Format(0.6));
        Assert.Equal("2.5", CardZoom.Format(2.5));
    }

    [Fact]
    public void The_stored_text_reads_the_same_on_a_Spanish_computer()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-AR");
        try
        {
            Assert.Equal("1.5", CardZoom.Format(1.5));
            Assert.Equal(1.5, CardZoom.Parse("1.5", CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_comma_is_not_a_decimal_point_in_the_stored_text()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-AR");
        try
        {
            Assert.Equal(CardZoom.Default, CardZoom.Parse("1,5", CardZoom.SlidesMin, CardZoom.SlidesMax));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void One_step_up_or_down_moves_by_a_tenth()
    {
        Assert.Equal(1.1, CardZoom.Nudge(1.0, +1, CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
        Assert.Equal(0.9, CardZoom.Nudge(1.0, -1, CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
    }

    [Fact]
    public void Repeated_steps_do_not_drift_away_from_the_tenths()
    {
        var size = CardZoom.Default;
        for (var i = 0; i < 7; i++)
            size = CardZoom.Nudge(size, +1, CardZoom.SlidesMin, CardZoom.SlidesMax);

        Assert.Equal(1.7, size, precision: 6);
        Assert.Equal("1.7", CardZoom.Format(size));
    }

    [Fact]
    public void A_step_stops_at_the_edge_of_the_range()
    {
        Assert.Equal(CardZoom.SlidesMin, CardZoom.Nudge(CardZoom.SlidesMin, -1, CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
        Assert.Equal(CardZoom.SlidesMax, CardZoom.Nudge(CardZoom.SlidesMax, +1, CardZoom.SlidesMin, CardZoom.SlidesMax), precision: 6);
    }

    [Fact]
    public void A_step_from_an_odd_value_lands_on_a_tenth()
    {
        Assert.Equal(1.1, CardZoom.Nudge(1.04, +1, CardZoom.MediaMin, CardZoom.MediaMax), precision: 6);
    }
}
