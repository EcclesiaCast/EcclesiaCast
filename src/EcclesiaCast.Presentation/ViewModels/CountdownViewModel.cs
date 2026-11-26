using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>
/// Sets up the "we start in 5:00" screen. Two ways to say when, because both
/// come up: a stretch of minutes when the band finishes the sound check, and
/// a time on the clock when the service has a fixed start.
/// </summary>
public sealed partial class CountdownViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _now;

    public CountdownViewModel(CountdownSettings settings, bool isRunning, Func<DateTimeOffset>? now = null)
    {
        _now = now ?? (() => DateTimeOffset.Now);
        IsRunning = isRunning;
        _minutes = settings.Minutes.ToString(CultureInfo.InvariantCulture);
        _clock = settings.Clock;
        _heading = settings.Heading;
        _finishedMessage = settings.FinishedMessage;
    }

    /// <summary>One is already on screen: there is something to stop.</summary>
    public bool IsRunning { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ByClock))]
    private bool _byMinutes = true;

    /// <summary>The other way of saying when.</summary>
    public bool ByClock
    {
        get => !ByMinutes;
        set
        {
            if (value)
                ByMinutes = false;
        }
    }

    [ObservableProperty]
    private string _minutes;

    [ObservableProperty]
    private string _clock;

    [ObservableProperty]
    private string _heading;

    [ObservableProperty]
    private string _finishedMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _error;

    public bool HasError => Error is not null;

    /// <summary>What the operator decided; null while they have not.</summary>
    public CountdownChoice? Choice { get; private set; }

    /// <summary>What the box of the minutes takes while typing.</summary>
    public static bool IsDigits(string text) => text.All(char.IsDigit);

    /// <summary>One of the usual stretches: «5», «10»…</summary>
    public void UsePreset(string minutes)
    {
        Minutes = minutes;
        ByMinutes = true;
    }

    /// <summary>
    /// «Proyectar»: true when the countdown can run (<see cref="Choice"/>),
    /// false when what was written does not read and <see cref="Error"/> says so.
    /// </summary>
    public bool Start()
    {
        var heading = (Heading ?? "").Trim();
        var finished = (FinishedMessage ?? "").Trim();
        var clockText = (Clock ?? "").Trim();
        var minutesText = (Minutes ?? "").Trim();
        var now = _now();

        Countdown countdown;
        if (ByClock)
        {
            if (!TimeOnly.TryParse(clockText, CultureInfo.CurrentCulture, out var clock))
                return Fail("Escribí la hora como 10:30 o 19:00.");

            countdown = Countdown.UntilClock(clock, now, heading, finished);
        }
        else
        {
            if (!int.TryParse(minutesText, out var minutes) || minutes <= 0)
                return Fail("Poné cuántos minutos faltan, por ejemplo 5.");

            countdown = Countdown.ForMinutes(minutes, now, heading, finished);
        }

        // What to remember for next Sunday.
        var settings = new CountdownSettings(
            int.TryParse(minutesText, out var saved) && saved > 0 ? saved : 5,
            clockText, heading, finished);
        Choice = new CountdownChoice(countdown, settings, Stop: false);
        return true;
    }

    /// <summary>«Detener la que corre».</summary>
    public void Stop() => Choice = new CountdownChoice(null, CountdownSettings.Default, Stop: true);

    private bool Fail(string message)
    {
        Error = message;
        return false;
    }
}
