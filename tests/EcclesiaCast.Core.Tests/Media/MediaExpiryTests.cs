using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class MediaExpiryTests
{
    private static MediaItem Poster(DateTime? eventDate, int days = 10, string category = "Anuncios") => new()
    {
        Name = "Afiche",
        Category = category,
        EventDate = eventDate,
        ArchiveAfterDays = days,
    };

    [Fact]
    public void A_poster_without_a_date_never_expires()
    {
        Assert.False(MediaExpiry.IsDue(Poster(null), new DateTime(2030, 1, 1)));
    }

    [Fact]
    public void It_stays_ten_days_after_the_event_and_goes_on_the_tenth()
    {
        var poster = Poster(new DateTime(2026, 10, 17));

        Assert.False(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 17)));
        Assert.False(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 26, 23, 59, 0)));
        Assert.True(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 27)));
        Assert.True(MediaExpiry.IsDue(poster, new DateTime(2026, 12, 1)));
        Assert.Equal(new DateTime(2026, 10, 27), MediaExpiry.MovesOn(poster.EventDate, poster.ArchiveAfterDays));
    }

    [Fact]
    public void The_number_of_days_is_the_posters_own()
    {
        var poster = Poster(new DateTime(2026, 10, 17), days: 0);

        Assert.True(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 17)));
        Assert.False(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 16)));
    }

    [Fact]
    public void A_negative_number_of_days_counts_as_zero()
    {
        Assert.Equal(new DateTime(2026, 10, 17), MediaExpiry.MovesOn(new DateTime(2026, 10, 17), -5));
    }

    [Fact]
    public void What_is_already_in_the_past_events_tab_is_not_moved_again()
    {
        var poster = Poster(new DateTime(2020, 1, 1), category: "eventos PASADOS");

        Assert.False(MediaExpiry.IsDue(poster, new DateTime(2026, 10, 17)));
    }

    [Theory]
    [InlineData("17/10/2026", 2026, 10, 17)]
    [InlineData("17-10-26", 2026, 10, 17)]
    [InlineData("1.2.27", 2027, 2, 1)]
    [InlineData(" 7/3/2026 ", 2026, 3, 7)]
    [InlineData("2026-10-17", 2026, 10, 17)]
    public void Dates_are_read_as_they_are_written_here(string text, int year, int month, int day)
    {
        Assert.True(MediaExpiry.TryParseDate(text, out var date));
        Assert.Equal(new DateTime(year, month, day), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("mañana")]
    [InlineData("32/1/2026")]
    [InlineData("10/17/2026")]
    public void Anything_else_is_not_a_date(string? text)
    {
        Assert.False(MediaExpiry.TryParseDate(text, out _));
    }

    [Fact]
    public void A_date_is_written_day_month_year()
    {
        Assert.Equal("07/03/2026", MediaExpiry.Format(new DateTime(2026, 3, 7)));
    }
}
