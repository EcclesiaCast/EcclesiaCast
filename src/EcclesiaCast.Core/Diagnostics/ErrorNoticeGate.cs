namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// Decides whether an unexpected error should be announced to the operator
/// right now. Errors that repeat must not flood the status bar: at most one
/// notice per interval. The clock is passed in so it can be tested.
/// </summary>
public sealed class ErrorNoticeGate(TimeSpan interval)
{
    private readonly object _gate = new();
    private DateTimeOffset? _lastNotice;

    /// <summary>True (and remembers it) when a notice may be shown at <paramref name="now"/>.</summary>
    public bool ShouldNotify(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_lastNotice is { } last && now - last < interval)
                return false;
            _lastNotice = now;
            return true;
        }
    }
}
