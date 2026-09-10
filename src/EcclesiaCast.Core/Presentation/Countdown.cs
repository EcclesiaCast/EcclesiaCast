namespace EcclesiaCast.Core.Presentation;

/// <summary>
/// The "we start in 5:00" screen that runs before a service.
///
/// It is pinned to a moment in time rather than to a number of seconds left,
/// so it stays honest: the projector can drop a few frames, the operator can
/// black the screen and bring it back, the stage display can start counting
/// halfway through, and every one of them still agrees on the clock.
/// </summary>
public sealed record Countdown(DateTimeOffset EndsAt, string Heading, string FinishedMessage)
{
    /// <summary>Starts a countdown that lasts <paramref name="minutes"/> from now.</summary>
    public static Countdown ForMinutes(double minutes, DateTimeOffset now, string heading, string finished) =>
        new(now.AddMinutes(minutes), heading, finished);

    /// <summary>
    /// Counts down to a time on the clock — "we start at 10:30". A time that
    /// already went by today is taken as tomorrow's, so setting up at 23:50
    /// for a 00:15 start does the obvious thing.
    /// </summary>
    public static Countdown UntilClock(TimeOnly clock, DateTimeOffset now, string heading, string finished)
    {
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, clock.Hour, clock.Minute, 0, now.Offset);
        return new Countdown(today > now ? today : today.AddDays(1), heading, finished);
    }

    public TimeSpan Remaining(DateTimeOffset now)
    {
        var left = EndsAt - now;
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    public bool HasFinished(DateTimeOffset now) => now >= EndsAt;

    /// <summary>
    /// What the congregation reads. Seconds are rounded up, so a countdown of
    /// five minutes reads 5:00 the moment it starts instead of flashing 4:59.
    /// </summary>
    public string Format(DateTimeOffset now)
    {
        if (HasFinished(now))
            return string.IsNullOrWhiteSpace(FinishedMessage) ? "0:00" : FinishedMessage;

        var left = TimeSpan.FromSeconds(Math.Ceiling(Remaining(now).TotalSeconds));

        return left.TotalHours >= 1
            ? $"{(int)left.TotalHours}:{left.Minutes:00}:{left.Seconds:00}"
            : $"{left.Minutes}:{left.Seconds:00}";
    }
}
