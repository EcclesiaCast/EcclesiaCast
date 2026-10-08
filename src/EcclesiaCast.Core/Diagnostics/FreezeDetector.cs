namespace EcclesiaCast.Core.Diagnostics;

/// <summary>What the detector noticed.</summary>
public enum FreezeEventKind
{
    /// <summary>The window stopped answering for longer than the threshold.</summary>
    Frozen,

    /// <summary>It answers again.</summary>
    Recovered,
}

public readonly record struct FreezeEvent(FreezeEventKind Kind, TimeSpan Duration);

/// <summary>
/// The bookkeeping behind the watchdog: a ping is sent to the window now and
/// then, and if it isn't answered within the threshold the window is frozen.
/// Kept apart from the thread that drives it so it can be tested with made-up
/// times.
/// </summary>
public sealed class FreezeDetector(TimeSpan threshold)
{
    private readonly object _gate = new();
    private DateTimeOffset? _pingSentAt;
    private bool _reported;

    /// <summary>True while a ping is out and unanswered.</summary>
    public bool PingPending
    {
        get { lock (_gate) return _pingSentAt is not null; }
    }

    /// <summary>
    /// Drops a ping in flight without reporting anything. Used when the watchdog
    /// itself was suspended (the computer slept): that is not the window's fault.
    /// </summary>
    public void Forgive()
    {
        lock (_gate)
        {
            _pingSentAt = null;
            _reported = false;
        }
    }

    /// <summary>A ping was posted to the window. Only the oldest unanswered one counts.</summary>
    public void PingSent(DateTimeOffset now)
    {
        lock (_gate)
            _pingSentAt ??= now;
    }

    /// <summary>
    /// Called by the watchdog between pings. Returns <see cref="FreezeEventKind.Frozen"/>
    /// once, the first time the pending ping is older than the threshold.
    /// </summary>
    public FreezeEvent? Check(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_pingSentAt is not { } since || _reported || now - since <= threshold)
                return null;

            _reported = true;
            return new FreezeEvent(FreezeEventKind.Frozen, now - since);
        }
    }

    /// <summary>
    /// The window answered. If a freeze was reported, returns
    /// <see cref="FreezeEventKind.Recovered"/> with how long it lasted in total.
    /// </summary>
    public FreezeEvent? PingAnswered(DateTimeOffset now)
    {
        lock (_gate)
        {
            var since = _pingSentAt;
            _pingSentAt = null;

            if (!_reported || since is null)
                return null;

            _reported = false;
            return new FreezeEvent(FreezeEventKind.Recovered, now - since.Value);
        }
    }
}
