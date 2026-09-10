using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Core.Tests.Presentation;

public class CountdownTests
{
    private static readonly DateTimeOffset Sunday =
        new(2026, 9, 13, 10, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void It_reads_the_full_time_the_moment_it_starts()
    {
        var countdown = Countdown.ForMinutes(5, Sunday, "Empezamos en", "");

        // Not 4:59: the congregation would see the number drop before it settled.
        Assert.Equal("5:00", countdown.Format(Sunday));
    }

    [Fact]
    public void It_counts_down_by_the_clock()
    {
        var countdown = Countdown.ForMinutes(5, Sunday, "Empezamos en", "");

        Assert.Equal("4:00", countdown.Format(Sunday.AddMinutes(1)));
        Assert.Equal("0:30", countdown.Format(Sunday.AddSeconds(270)));
        Assert.Equal("0:01", countdown.Format(Sunday.AddSeconds(299)));
    }

    [Fact]
    public void Long_countdowns_show_the_hours()
    {
        var countdown = Countdown.ForMinutes(65, Sunday, "Empezamos en", "");

        Assert.Equal("1:05:00", countdown.Format(Sunday));
    }

    [Fact]
    public void At_zero_it_says_what_the_operator_wrote()
    {
        var countdown = Countdown.ForMinutes(5, Sunday, "Empezamos en", "¡Bienvenidos!");

        Assert.True(countdown.HasFinished(Sunday.AddMinutes(5)));
        Assert.Equal("¡Bienvenidos!", countdown.Format(Sunday.AddMinutes(5)));
        // And it stays there rather than counting into the negatives.
        Assert.Equal("¡Bienvenidos!", countdown.Format(Sunday.AddHours(2)));
    }

    [Fact]
    public void Without_a_closing_message_it_rests_at_zero()
    {
        var countdown = Countdown.ForMinutes(5, Sunday, "Empezamos en", "");

        Assert.Equal("0:00", countdown.Format(Sunday.AddMinutes(6)));
        Assert.Equal(TimeSpan.Zero, countdown.Remaining(Sunday.AddMinutes(6)));
    }

    [Fact]
    public void Counting_to_a_time_on_the_clock()
    {
        var countdown = Countdown.UntilClock(new TimeOnly(10, 30), Sunday, "Empezamos en", "");

        Assert.Equal("30:00", countdown.Format(Sunday));
    }

    [Fact]
    public void A_clock_time_already_gone_by_means_tomorrow()
    {
        // Setting up at 23:50 for a service that starts at 00:15.
        var lateNight = new DateTimeOffset(2026, 9, 13, 23, 50, 0, TimeSpan.FromHours(-3));

        var countdown = Countdown.UntilClock(new TimeOnly(0, 15), lateNight, "Empezamos en", "");

        Assert.Equal("25:00", countdown.Format(lateNight));
    }

    [Fact]
    public void Blacking_the_screen_does_not_pause_it()
    {
        // The countdown is a moment in time, not a ticking number: whatever
        // happens to the output, everyone reads the same thing.
        var countdown = Countdown.ForMinutes(5, Sunday, "Empezamos en", "");

        Assert.Equal(countdown.Format(Sunday.AddMinutes(3)), countdown.Format(Sunday.AddMinutes(3)));
        Assert.Equal("2:00", countdown.Format(Sunday.AddMinutes(3)));
    }
}
