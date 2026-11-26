using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Presentation.Tests;

public sealed class CountdownViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 29, 9, 0, 0, TimeSpan.FromHours(-3));

    private static CountdownViewModel New(bool isRunning = false) =>
        new(CountdownSettings.Default, isRunning, () => Now);

    [Fact]
    public void It_opens_with_what_was_typed_last_time()
    {
        var countdown = new CountdownViewModel(new CountdownSettings(12, "19:00", "Falta", "Ya"), isRunning: true);

        Assert.Equal("12", countdown.Minutes);
        Assert.Equal("19:00", countdown.Clock);
        Assert.Equal("Falta", countdown.Heading);
        Assert.Equal("Ya", countdown.FinishedMessage);
        Assert.True(countdown.IsRunning);
        Assert.True(countdown.ByMinutes);
        Assert.False(countdown.HasError);
        Assert.Null(countdown.Choice);
    }

    [Fact]
    public void Minutes_from_now_with_the_texts_trimmed()
    {
        var countdown = New();
        countdown.Minutes = " 15 ";
        countdown.Heading = "  Empezamos en ";

        Assert.True(countdown.Start());

        var choice = countdown.Choice!;
        Assert.False(choice.Stop);
        Assert.Equal(Now.AddMinutes(15), choice.Countdown!.EndsAt);
        Assert.Equal("Empezamos en", choice.Countdown.Heading);
        Assert.Equal(new CountdownSettings(15, "10:30", "Empezamos en", "¡Bienvenidos!"), choice.Settings);
    }

    [Fact]
    public void A_time_on_the_clock_remembers_five_minutes_if_the_box_does_not_read()
    {
        var countdown = New();
        countdown.ByClock = true;
        countdown.Minutes = "";
        countdown.Clock = "10:30";

        Assert.True(countdown.Start());

        Assert.Equal(Now.AddMinutes(90), countdown.Choice!.Countdown!.EndsAt);
        Assert.Equal(5, countdown.Choice.Settings.Minutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("cinco")]
    public void Without_minutes_it_says_so(string minutes)
    {
        var countdown = New();
        countdown.Minutes = minutes;

        Assert.False(countdown.Start());

        Assert.Equal("Poné cuántos minutos faltan, por ejemplo 5.", countdown.Error);
        Assert.True(countdown.HasError);
        Assert.Null(countdown.Choice);
    }

    [Fact]
    public void A_time_that_does_not_read_says_how_to_write_it()
    {
        var countdown = New();
        countdown.ByClock = true;
        countdown.Clock = "a la tarde";

        Assert.False(countdown.Start());

        Assert.Equal("Escribí la hora como 10:30 o 19:00.", countdown.Error);
    }

    [Fact]
    public void A_preset_goes_back_to_minutes()
    {
        var countdown = New();
        countdown.ByClock = true;

        countdown.UsePreset("30");

        Assert.Equal("30", countdown.Minutes);
        Assert.True(countdown.ByMinutes);
        Assert.False(countdown.ByClock);
    }

    [Fact]
    public void Unchecking_a_way_does_not_choose_the_other()
    {
        var countdown = New();

        countdown.ByClock = false;

        Assert.True(countdown.ByMinutes);
    }

    [Fact]
    public void Stopping_asks_to_stop()
    {
        var countdown = New(isRunning: true);

        countdown.Stop();

        Assert.True(countdown.Choice!.Stop);
        Assert.Null(countdown.Choice.Countdown);
    }

    [Theory]
    [InlineData("7", true)]
    [InlineData("a", false)]
    [InlineData(" ", false)]
    public void The_minutes_box_takes_digits(string typed, bool taken) =>
        Assert.Equal(taken, CountdownViewModel.IsDigits(typed));
}
