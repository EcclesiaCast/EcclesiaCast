namespace EcclesiaCast.Core.Diagnostics;

/// <summary>
/// "Press twice" rule: the first press only warns, a second one within the
/// window confirms. After the window it asks for two again. The clock is
/// passed in so it can be tested.
/// </summary>
public sealed class DoublePressGate(TimeSpan window)
{
    private DateTimeOffset? _firstPress;

    /// <summary>True when this press confirms (and resets); false when it is the first one.</summary>
    public bool Press(DateTimeOffset now)
    {
        if (_firstPress is { } first && now - first <= window)
        {
            _firstPress = null;
            return true;
        }
        _firstPress = now;
        return false;
    }

    /// <summary>Forgets a pending first press.</summary>
    public void Reset() => _firstPress = null;
}
