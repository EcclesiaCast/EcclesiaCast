using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.Core.Tests.Updates;

public class UpdateScheduleTests
{
    private static readonly DateTimeOffset Morning = new(2026, 10, 11, 9, 30, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void A_program_that_never_looked_checks()
    {
        Assert.True(UpdateSchedule.ShouldCheck(new MemorySettings(), Morning, isDevelopmentBuild: false));
    }

    [Fact]
    public void It_does_not_check_twice_the_same_day()
    {
        var settings = new MemorySettings();
        UpdateSchedule.MarkChecked(settings, Morning);

        Assert.False(UpdateSchedule.ShouldCheck(settings, Morning.AddHours(10), isDevelopmentBuild: false));
    }

    [Fact]
    public void It_checks_again_the_next_day()
    {
        var settings = new MemorySettings();
        UpdateSchedule.MarkChecked(settings, Morning);

        Assert.True(UpdateSchedule.ShouldCheck(settings, Morning.AddDays(1), isDevelopmentBuild: false));
    }

    [Fact]
    public void A_day_is_a_calendar_day_not_twenty_four_hours()
    {
        var settings = new MemorySettings();
        var lateNight = new DateTimeOffset(2026, 10, 11, 23, 50, 0, TimeSpan.FromHours(-3));
        UpdateSchedule.MarkChecked(settings, lateNight);

        Assert.True(UpdateSchedule.ShouldCheck(settings, lateNight.AddMinutes(20), isDevelopmentBuild: false));
    }

    [Fact]
    public void It_checks_after_a_clock_that_went_backwards()
    {
        var settings = new MemorySettings();
        UpdateSchedule.MarkChecked(settings, Morning.AddDays(40));

        Assert.True(UpdateSchedule.ShouldCheck(settings, Morning, isDevelopmentBuild: false));
    }

    [Fact]
    public void It_never_checks_when_the_operator_turned_it_off()
    {
        var settings = new MemorySettings();
        UpdateSchedule.SetEnabled(settings, false);

        Assert.False(UpdateSchedule.ShouldCheck(settings, Morning, isDevelopmentBuild: false));
        Assert.False(UpdateSchedule.IsEnabled(settings));
    }

    [Fact]
    public void Turning_it_back_on_makes_it_check_again()
    {
        var settings = new MemorySettings();
        UpdateSchedule.SetEnabled(settings, false);
        UpdateSchedule.SetEnabled(settings, true);

        Assert.True(UpdateSchedule.ShouldCheck(settings, Morning, isDevelopmentBuild: false));
    }

    [Fact]
    public void It_is_on_when_nothing_was_ever_chosen()
    {
        Assert.True(UpdateSchedule.IsEnabled(new MemorySettings()));
    }

    [Fact]
    public void It_never_checks_in_a_development_build()
    {
        Assert.False(UpdateSchedule.ShouldCheck(new MemorySettings(), Morning, isDevelopmentBuild: true));
    }

    [Fact]
    public void A_garbled_date_in_the_settings_counts_as_never_checked()
    {
        var settings = new MemorySettings();
        settings.Set(UpdateSchedule.LastCheckKey, "ayer");

        Assert.True(UpdateSchedule.ShouldCheck(settings, Morning, isDevelopmentBuild: false));
    }

    [Fact]
    public void The_skipped_version_is_remembered_as_a_version()
    {
        var settings = new MemorySettings();
        Assert.Null(UpdateSchedule.Skipped(settings));

        UpdateSchedule.Skip(settings, AppVersion.TryParse("v1.0.1")!);

        Assert.Equal(AppVersion.TryParse("1.0.1"), UpdateSchedule.Skipped(settings));
    }

    [Fact]
    public void Skipping_a_newer_version_replaces_the_older_skip()
    {
        var settings = new MemorySettings();
        UpdateSchedule.Skip(settings, AppVersion.TryParse("1.0.1")!);
        UpdateSchedule.Skip(settings, AppVersion.TryParse("1.0.2")!);

        Assert.Equal(AppVersion.TryParse("1.0.2"), UpdateSchedule.Skipped(settings));
    }

    [Fact]
    public void The_search_starts_a_few_seconds_after_opening()
    {
        Assert.Equal(TimeSpan.FromSeconds(15), UpdateSchedule.StartDelay);
    }
}
