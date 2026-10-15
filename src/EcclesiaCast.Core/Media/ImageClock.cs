namespace EcclesiaCast.Core.Media;

/// <summary>
/// The time an image stays on the screen when it has seconds set, kept like a
/// video's: it can be paused, moved and started over from the same playback
/// bar, and it says when it is over. Times are passed in, so it can be
/// tested without waiting.
/// </summary>
public sealed class ImageClock
{
    private TimeSpan _elapsedBefore;
    private DateTime? _runningSince;

    public ImageClock(TimeSpan duration, DateTime now)
    {
        Duration = duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(1);
        _runningSince = now;
    }

    public TimeSpan Duration { get; }

    public bool IsRunning => _runningSince is not null;

    /// <summary>How long it has been on the screen, never past <see cref="Duration"/>.</summary>
    public TimeSpan Elapsed(DateTime now)
    {
        var running = _runningSince is { } since && now > since ? now - since : TimeSpan.Zero;
        var total = _elapsedBefore + running;
        return total > Duration ? Duration : total;
    }

    public bool IsOver(DateTime now) => Elapsed(now) >= Duration;

    public void Pause(DateTime now)
    {
        if (_runningSince is null)
            return;

        _elapsedBefore = Elapsed(now);
        _runningSince = null;
    }

    public void Resume(DateTime now)
    {
        if (_runningSince is null && !IsOver(now))
            _runningSince = now;
    }

    public void TogglePause(DateTime now)
    {
        if (IsRunning)
            Pause(now);
        else
            Resume(now);
    }

    /// <summary>Puts it at <paramref name="position"/> (kept inside the length), running or paused as it was.</summary>
    public void Seek(TimeSpan position, DateTime now)
    {
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        if (position > Duration)
            position = Duration;

        _elapsedBefore = position;
        if (_runningSince is not null)
            _runningSince = now;
    }
}
