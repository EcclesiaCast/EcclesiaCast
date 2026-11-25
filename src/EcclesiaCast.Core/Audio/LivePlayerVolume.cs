namespace EcclesiaCast.Core.Audio;

/// <summary>
/// The volume of a player whose track can change under it. Every new value
/// reaches the player inside the same call (no timer, no other thread, nothing
/// that waits for the mouse to be let go), and a new player gets the current
/// one as soon as it is attached, before it plays.
/// </summary>
public sealed class LivePlayerVolume(int initial)
{
    private int _value = Math.Clamp(initial, 0, 100);
    private Action<int>? _apply;

    /// <summary>0 to 100.</summary>
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            _apply?.Invoke(_value);
        }
    }

    /// <summary>Call with each new player BEFORE it plays: it must never start at another volume.</summary>
    public void Attach(Action<int> apply)
    {
        _apply = apply;
        apply(_value);
    }

    /// <summary>Call before the player is thrown away: nothing may touch it afterwards.</summary>
    public void Detach() => _apply = null;
}
