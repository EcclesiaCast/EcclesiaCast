using EcclesiaCast.Core.Timing;

namespace EcclesiaCast.Core.Tests.Timing;

public class RunClockTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void It_counts_only_while_it_runs()
    {
        var clock = new RunClock();

        clock.Start(T0);
        clock.Pause(T0.AddSeconds(90));

        Assert.Equal(TimeSpan.FromSeconds(90), clock.Elapsed(T0.AddMinutes(10)));
        Assert.False(clock.IsRunning);
    }

    [Fact]
    public void Starting_again_goes_on_from_where_it_was()
    {
        var clock = new RunClock();
        clock.Start(T0);
        clock.Pause(T0.AddSeconds(30));

        clock.Start(T0.AddMinutes(5));

        Assert.Equal(TimeSpan.FromSeconds(30 + 20), clock.Elapsed(T0.AddMinutes(5).AddSeconds(20)));
    }

    [Fact]
    public void Starting_twice_does_not_restart_the_count()
    {
        var clock = new RunClock();
        clock.Start(T0);

        clock.Start(T0.AddSeconds(40));

        Assert.Equal(TimeSpan.FromSeconds(60), clock.Elapsed(T0.AddSeconds(60)));
    }

    [Fact]
    public void Reset_leaves_it_at_zero_and_stopped()
    {
        var clock = new RunClock();
        clock.Start(T0);

        clock.Reset();

        Assert.False(clock.IsRunning);
        Assert.Equal(TimeSpan.Zero, clock.Elapsed(T0.AddHours(1)));
    }

    [Fact]
    public void A_computer_clock_set_back_counts_nothing_instead_of_minus()
    {
        var clock = new RunClock();
        clock.Start(T0);

        Assert.Equal(TimeSpan.Zero, clock.Elapsed(T0.AddMinutes(-5)));
    }
}

public class SectionTimerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    private static SectionTimer Alabanza()
    {
        var timer = new SectionTimer();
        timer.Load(TimeSpan.FromMinutes(25));
        return timer;
    }

    [Fact]
    public void A_section_starts_stopped_with_all_its_time()
    {
        var timer = Alabanza();

        Assert.False(timer.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(T0.AddHours(1)));
    }

    [Fact]
    public void It_counts_down_while_it_runs()
    {
        var timer = Alabanza();
        timer.Start(T0);

        Assert.Equal(TimeSpan.FromMinutes(20), timer.Remaining(T0.AddMinutes(5)));
    }

    [Fact]
    public void Pausing_freezes_what_is_left()
    {
        var timer = Alabanza();
        timer.Start(T0);
        timer.Pause(T0.AddMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(20), timer.Remaining(T0.AddHours(2)));
    }

    [Fact]
    public void Past_the_time_it_goes_on_counting_below_zero()
    {
        var timer = Alabanza();
        timer.Start(T0);

        var now = T0.AddMinutes(27).AddSeconds(15);

        Assert.True(timer.IsOvertime(now));
        Assert.Equal(-TimeSpan.FromSeconds(135), timer.Remaining(now));
    }

    [Fact]
    public void It_is_not_overtime_until_the_time_is_used_up()
    {
        var timer = Alabanza();
        timer.Start(T0);

        Assert.False(timer.IsOvertime(T0.AddMinutes(25)));
        Assert.True(timer.IsOvertime(T0.AddMinutes(25).AddSeconds(1)));
    }

    [Fact]
    public void Adding_and_taking_a_minute_moves_what_is_left()
    {
        var timer = Alabanza();
        timer.Start(T0);

        timer.AddMinutes(1);
        timer.AddMinutes(1);
        timer.AddMinutes(-1);

        Assert.Equal(TimeSpan.FromMinutes(26 - 4), timer.Remaining(T0.AddMinutes(4)));
    }

    [Fact]
    public void A_minute_added_can_pull_it_out_of_overtime()
    {
        var timer = Alabanza();
        timer.Start(T0);
        var late = T0.AddMinutes(25).AddSeconds(30);

        timer.AddMinutes(1);

        Assert.False(timer.IsOvertime(late));
    }

    [Fact]
    public void Reset_returns_the_planned_time_and_forgets_the_minutes_added()
    {
        var timer = Alabanza();
        timer.Start(T0);
        timer.AddMinutes(5);

        timer.Reset();

        Assert.False(timer.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(T0.AddMinutes(9)));
    }

    [Fact]
    public void Loading_the_next_section_starts_it_over()
    {
        var timer = Alabanza();
        timer.Start(T0);
        timer.AddMinutes(3);

        timer.Load(TimeSpan.FromMinutes(5));

        Assert.False(timer.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(T0.AddMinutes(9)));
    }
}

public class TimeTextTests
{
    [Theory]
    [InlineData(1500, "25:00")]
    [InlineData(247, "4:07")]
    [InlineData(3900, "1:05:00")]
    [InlineData(0, "0:00")]
    [InlineData(-135, "-2:15")]
    [InlineData(-3700, "-1:01:40")]
    public void A_time_is_written_in_minutes_and_seconds(int seconds, string expected)
    {
        Assert.Equal(expected, TimeText.Clock(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void A_countdown_shows_the_second_it_is_still_in()
    {
        Assert.Equal("25:00", TimeText.Clock(TimeSpan.FromSeconds(1499.6)));
        Assert.Equal("0:01", TimeText.Clock(TimeSpan.FromSeconds(0.3)));
    }

    [Fact]
    public void The_first_second_of_overrun_reads_minus_one()
    {
        Assert.Equal("-0:01", TimeText.Clock(TimeSpan.FromSeconds(-0.3)));
    }

    [Fact]
    public void A_stopwatch_never_shows_a_negative_time()
    {
        Assert.Equal("0:00", TimeText.Elapsed(TimeSpan.FromSeconds(-5)));
        Assert.Equal("12:45", TimeText.Elapsed(TimeSpan.FromSeconds(765.9)));
    }
}

public class MeetingPlanTests
{
    [Fact]
    public void With_nothing_saved_the_church_starts_with_the_usual_parts()
    {
        var sections = MeetingPlan.Deserialize(null);

        Assert.Equal(["Alabanza", "Anuncios", "Prédica"], sections.Select(s => s.Name));
        Assert.Equal([25, 5, 35], sections.Select(s => s.Minutes));
    }

    [Fact]
    public void The_sections_are_saved_and_read_back_in_order()
    {
        var mine = new List<MeetingSection> { new("Oración", 10), new("🎶 Alabanza", 20), new("Cena", 15) };

        Assert.Equal(mine, MeetingPlan.Deserialize(MeetingPlan.Serialize(mine)));
    }

    [Fact]
    public void An_unreadable_setting_falls_back_to_the_usual_parts()
    {
        Assert.Equal(3, MeetingPlan.Deserialize("esto no es json").Count);
    }

    [Fact]
    public void A_deleted_list_stays_empty_instead_of_coming_back()
    {
        Assert.Empty(MeetingPlan.Deserialize(MeetingPlan.Serialize([])));
    }

    [Fact]
    public void A_section_without_a_name_or_with_absurd_minutes_is_tidied()
    {
        var tidy = MeetingPlan.Clean(new MeetingSection("   ", 99999));

        Assert.Equal("Sección", tidy.Name);
        Assert.Equal(MeetingPlan.MaxMinutes, tidy.Minutes);
        Assert.Equal(0, MeetingPlan.Clean(new MeetingSection("X", -5)).Minutes);
    }

    [Fact]
    public void The_next_section_is_none_after_the_last_one()
    {
        Assert.Equal(1, MeetingPlan.NextIndex(0, 3));
        Assert.Null(MeetingPlan.NextIndex(2, 3));
    }
}
