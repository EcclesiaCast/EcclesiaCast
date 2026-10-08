namespace EcclesiaCast.Core.Audio;

/// <summary>
/// Which track comes next or before in the list of the Música tab: the rules
/// of the previous / next buttons and of what happens when a track ends.
/// </summary>
public static class AudioNavigator
{
    /// <summary>Past this many seconds, «anterior» starts the same track over instead of going back.</summary>
    public const double RestartAfterSeconds = 3;

    /// <summary>
    /// The track after <paramref name="current"/>, or null at the end of the
    /// list: the music does not loop on its own, so a service does not go on
    /// with the first song after the last one. With nothing playing yet
    /// (<c>-1</c>) it is the first.
    /// </summary>
    public static int? Next(int count, int current)
    {
        if (count <= 0)
            return null;

        var next = current + 1;
        return next < count ? next : null;
    }

    /// <summary>
    /// The track «anterior» goes to. Like any player, a few seconds in it
    /// starts the same track over; only at the start does it go back (and the
    /// first track just starts over). Null when the list is empty.
    /// </summary>
    public static int? Previous(int count, int current, double positionSeconds)
    {
        if (count <= 0)
            return null;

        if (current < 0 || current >= count)
            return 0;

        if (positionSeconds > RestartAfterSeconds || current == 0)
            return current;

        return current - 1;
    }
}
