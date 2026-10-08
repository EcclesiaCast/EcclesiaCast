namespace EcclesiaCast.Core.Playlists;

/// <summary>What an arrow press should do.</summary>
public enum ArrowAction
{
    /// <summary>Go to the neighbouring slide of the same item.</summary>
    Move,

    /// <summary>Leave the item for the next (or previous) one of the playlist.</summary>
    Cross,

    /// <summary>At the edge of the item: stay put and say what another press does.</summary>
    Hold,

    /// <summary>The key is still held from a press that already did something: do nothing.</summary>
    Ignore,
}

/// <summary>
/// Keeps a held arrow key from running through the playlist. The keyboard
/// repeats a held key about thirty times a second, so without this the repeats
/// walked past the end of a passage, put a video on as background and went on
/// to the next item before anyone could let go. Reaching the edge of an item
/// stops the arrow; only a new press (not the keyboard's repetition) crosses,
/// and the repetition of that press that crossed is ignored.
/// </summary>
public sealed class ArrowStepGate
{
    private bool _ignoreRepeats;

    /// <param name="atEdge">The press would step out of the current playlist item.</param>
    /// <param name="hasNeighbour">There is an item to step into in that direction.</param>
    /// <param name="isRepeat">The keyboard's auto-repeat, not a new press.</param>
    public ArrowAction Decide(bool atEdge, bool hasNeighbour, bool isRepeat)
    {
        if (!isRepeat)
            _ignoreRepeats = false;
        else if (_ignoreRepeats)
            return ArrowAction.Ignore;

        // With nothing beyond, the operator can keep reading past the passage.
        if (!atEdge || !hasNeighbour)
            return ArrowAction.Move;

        if (isRepeat)
            return ArrowAction.Hold;

        _ignoreRepeats = true;
        return ArrowAction.Cross;
    }
}
