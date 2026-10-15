using EcclesiaCast.Core.Media;

namespace EcclesiaCast.Core.Tests.Media;

public class ImageClockTests
{
    private static readonly DateTime T0 = new(2026, 10, 15, 20, 0, 0);

    [Fact]
    public void It_counts_from_the_moment_the_image_goes_up_and_is_over_at_its_length()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(15), T0);

        Assert.Equal(TimeSpan.FromSeconds(5), clock.Elapsed(T0.AddSeconds(5)));
        Assert.False(clock.IsOver(T0.AddSeconds(14.9)));
        Assert.True(clock.IsOver(T0.AddSeconds(15)));
        Assert.Equal(TimeSpan.FromSeconds(15), clock.Elapsed(T0.AddMinutes(3)));
    }

    [Fact]
    public void Paused_it_stops_counting_and_picks_up_where_it_was()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(15), T0);
        clock.Pause(T0.AddSeconds(4));

        Assert.False(clock.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(4), clock.Elapsed(T0.AddSeconds(60)));

        clock.Resume(T0.AddSeconds(60));
        Assert.Equal(TimeSpan.FromSeconds(7), clock.Elapsed(T0.AddSeconds(63)));
        Assert.True(clock.IsOver(T0.AddSeconds(71)));
    }

    [Fact]
    public void Toggling_pauses_and_resumes()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(10), T0);
        clock.TogglePause(T0.AddSeconds(2));
        Assert.False(clock.IsRunning);
        clock.TogglePause(T0.AddSeconds(5));
        Assert.True(clock.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(3), clock.Elapsed(T0.AddSeconds(6)));
    }

    [Fact]
    public void Moving_it_keeps_it_running_from_the_new_place()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(30), T0);
        clock.Seek(TimeSpan.FromSeconds(20), T0.AddSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(23), clock.Elapsed(T0.AddSeconds(5)));
    }

    [Fact]
    public void Moving_it_while_paused_leaves_it_paused()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(30), T0);
        clock.Pause(T0.AddSeconds(1));
        clock.Seek(TimeSpan.FromSeconds(10), T0.AddSeconds(2));

        Assert.False(clock.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(10), clock.Elapsed(T0.AddSeconds(50)));
    }

    [Fact]
    public void It_cannot_be_moved_outside_its_length()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(30), T0);
        clock.Seek(TimeSpan.FromSeconds(-5), T0);
        Assert.Equal(TimeSpan.Zero, clock.Elapsed(T0));

        clock.Seek(TimeSpan.FromSeconds(99), T0);
        Assert.True(clock.IsOver(T0));
    }

    [Fact]
    public void An_image_that_is_over_does_not_start_again_by_resuming()
    {
        var clock = new ImageClock(TimeSpan.FromSeconds(5), T0);
        clock.Pause(T0.AddSeconds(9));
        clock.Resume(T0.AddSeconds(10));

        Assert.False(clock.IsRunning);
        Assert.True(clock.IsOver(T0.AddSeconds(10)));
    }

    [Fact]
    public void A_length_of_zero_or_less_is_taken_as_one_second()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), new ImageClock(TimeSpan.Zero, T0).Duration);
    }
}
