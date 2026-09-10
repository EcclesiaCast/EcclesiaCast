namespace EcclesiaCast.Core.Remote;

/// <summary>
/// Slows down anyone trying PIN after PIN against the phone remote.
///
/// The remote is protected by four digits, which is ten thousand
/// combinations: a script on the church wifi walks through them in seconds if
/// nothing stands in the way. Locking the offender out for a few minutes after
/// a handful of misses turns those seconds into days without ever getting in
/// the way of a volunteer who fat-fingered the PIN twice.
///
/// Attempts are counted per client address, so one phone locking itself out
/// never locks out the rest of the team.
/// </summary>
public sealed class PinAttemptLimiter
{
    private readonly Dictionary<string, Attempts> _byClient = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockout;
    private readonly TimeSpan _window;

    /// <param name="maxAttempts">Misses allowed before the lockout starts.</param>
    /// <param name="lockout">How long a locked-out client has to wait.</param>
    /// <param name="window">
    /// Misses further apart than this are treated as unrelated, so a wrong PIN
    /// on three different Sundays never adds up to a lockout.
    /// </param>
    public PinAttemptLimiter(int maxAttempts = 5, TimeSpan? lockout = null, TimeSpan? window = null)
    {
        _maxAttempts = Math.Max(1, maxAttempts);
        _lockout = lockout ?? TimeSpan.FromMinutes(5);
        _window = window ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// How long <paramref name="client"/> still has to wait, or null when it is
    /// free to try.
    /// </summary>
    public TimeSpan? RetryAfter(string client, DateTimeOffset now)
    {
        if (!_byClient.TryGetValue(client, out var attempts) || attempts.LockedUntil is not { } until)
            return null;

        if (until > now)
            return until - now;

        // The wait is over: the client starts again from zero rather than one
        // miss away from another lockout.
        _byClient.Remove(client);
        return null;
    }

    /// <summary>Records a wrong PIN, which may start the lockout.</summary>
    public void RecordFailure(string client, DateTimeOffset now)
    {
        Forget(now);

        if (!_byClient.TryGetValue(client, out var attempts) || now - attempts.LastFailure > _window)
            attempts = new Attempts();

        attempts = attempts with { Count = attempts.Count + 1, LastFailure = now };

        if (attempts.Count >= _maxAttempts)
            attempts = attempts with { LockedUntil = now + _lockout };

        _byClient[client] = attempts;
    }

    /// <summary>Records a correct PIN, clearing whatever the client had against it.</summary>
    public void RecordSuccess(string client) => _byClient.Remove(client);

    /// <summary>
    /// Forgets every client. Whoever can reach the computer to restart the
    /// remote is already standing at it, so a volunteer who locked their own
    /// phone out has a way back in that does not involve waiting.
    /// </summary>
    public void Clear() => _byClient.Clear();

    /// <summary>Clients currently being tracked. Exists for the tests.</summary>
    public int TrackedClients => _byClient.Count;

    /// <summary>
    /// Drops clients that are neither locked out nor within the window, so a
    /// flood of forged addresses cannot grow the table without bound.
    /// </summary>
    private void Forget(DateTimeOffset now)
    {
        if (_byClient.Count < 256)
            return;

        foreach (var (client, attempts) in _byClient.ToList())
        {
            var alive = attempts.LockedUntil is { } until ? until > now : now - attempts.LastFailure <= _window;
            if (!alive)
                _byClient.Remove(client);
        }
    }

    private readonly record struct Attempts(int Count, DateTimeOffset LastFailure, DateTimeOffset? LockedUntil);
}
