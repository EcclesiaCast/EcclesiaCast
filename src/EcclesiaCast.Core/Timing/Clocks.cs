namespace EcclesiaCast.Core.Timing;

/// <summary>
/// A stopwatch that can be started, paused and set to zero. It knows no clock
/// of its own: whoever asks says what time it is, so it can be tested and so
/// every screen that shows it reads the same instant.
/// </summary>
public sealed class RunClock
{
    private TimeSpan _before;
    private DateTimeOffset? _since;

    public bool IsRunning => _since is not null;

    public void Start(DateTimeOffset now) => _since ??= now;

    public void Pause(DateTimeOffset now)
    {
        if (_since is { } started)
        {
            _before += Passed(started, now);
            _since = null;
        }
    }

    /// <summary>Back to zero and stopped.</summary>
    public void Reset()
    {
        _before = TimeSpan.Zero;
        _since = null;
    }

    public TimeSpan Elapsed(DateTimeOffset now) =>
        _before + (_since is { } started ? Passed(started, now) : TimeSpan.Zero);

    // A clock that went back (the computer's time was changed) counts nothing, not minus.
    private static TimeSpan Passed(DateTimeOffset since, DateTimeOffset now) =>
        now > since ? now - since : TimeSpan.Zero;
}

/// <summary>
/// The time of one part of the meeting (Alabanza, Prédica…): it counts down
/// from what was planned, and once that is used up it goes on counting below
/// zero, so whoever is on the stage sees how much they have overrun.
/// </summary>
public sealed class SectionTimer
{
    private readonly RunClock _clock = new();
    private TimeSpan _planned;
    private TimeSpan _adjustment;

    public bool IsRunning => _clock.IsRunning;

    /// <summary>The time the section had when it was chosen, without the minutes added or taken since.</summary>
    public TimeSpan Planned => _planned;

    /// <summary>Starts a section: its planned time, stopped, with nothing added or taken.</summary>
    public void Load(TimeSpan planned)
    {
        _planned = planned < TimeSpan.Zero ? TimeSpan.Zero : planned;
        _adjustment = TimeSpan.Zero;
        _clock.Reset();
    }

    public void Start(DateTimeOffset now) => _clock.Start(now);

    public void Pause(DateTimeOffset now) => _clock.Pause(now);

    /// <summary>Back to the planned time, stopped.</summary>
    public void Reset()
    {
        _adjustment = TimeSpan.Zero;
        _clock.Reset();
    }

    /// <summary>Gives (positive) or takes away (negative) minutes from what is left.</summary>
    public void AddMinutes(int minutes) => _adjustment += TimeSpan.FromMinutes(minutes);

    /// <summary>What is left; below zero once the time is used up.</summary>
    public TimeSpan Remaining(DateTimeOffset now) => _planned + _adjustment - _clock.Elapsed(now);

    public bool IsOvertime(DateTimeOffset now) => Remaining(now) < TimeSpan.Zero;
}

/// <summary>
/// The two clocks of the meeting: the service stopwatch and the timer of the
/// section that is running. One instance is shared by the operator's Tiempos
/// tab and the stage screens, so they never disagree.
/// </summary>
public sealed class MeetingClocks
{
    public MeetingClocks()
    {
        // The service stopwatch has always been counting from the moment the
        // program opened; the Tiempos tab can pause it and set it to zero.
        Stopwatch.Start(DateTimeOffset.Now);
    }

    public RunClock Stopwatch { get; } = new();

    public SectionTimer Section { get; } = new();

    /// <summary>Name of the section the timer is on; empty when none is chosen.</summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>True once a section was chosen; before that the timer has nothing to show.</summary>
    public bool HasSection { get; set; }
}

/// <summary>How a time is written on the screens.</summary>
public static class TimeText
{
    /// <summary>
    /// «25:00», «4:07», «1:05:00», and «-2:15» once it is below zero. A
    /// countdown shows the second it is still in until it is over, so «0:00»
    /// is the moment the time is used up, and the first overrun second reads
    /// «-0:01».
    /// </summary>
    public static string Clock(TimeSpan time)
    {
        var negative = time < TimeSpan.Zero;
        var seconds = (long)(negative ? Math.Floor(time.TotalSeconds) : Math.Ceiling(time.TotalSeconds));
        var shown = TimeSpan.FromSeconds(Math.Abs(seconds));
        var text = shown.TotalHours >= 1 ? shown.ToString(@"h\:mm\:ss") : shown.ToString(@"m\:ss");
        return negative ? "-" + text : text;
    }

    /// <summary>A stopwatch counting up: the same shape, never negative.</summary>
    public static string Elapsed(TimeSpan time) =>
        Clock(time < TimeSpan.Zero ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Floor(time.TotalSeconds)));
}
