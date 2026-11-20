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
    /// What plays when a track ends by itself, by the repeat button: without
    /// repeat it is <see cref="Next(int,int)"/>; «repetir esta» plays the same
    /// one again; «repetir la lista» goes back to the first after the last.
    /// Null means the music stops there.
    /// </summary>
    public static int? AfterEnd(int count, int current, AudioRepeatMode repeat)
    {
        if (count <= 0)
            return null;

        return repeat switch
        {
            AudioRepeatMode.One when current >= 0 && current < count => current,
            AudioRepeatMode.All => current + 1 < count ? Math.Max(current + 1, 0) : 0,
            _ => Next(count, current),
        };
    }

    /// <summary>
    /// The ⏭ button: like <see cref="Next(int,int)"/>, but with the whole list
    /// on repeat the last one goes on to the first. «Repetir esta» does not
    /// hold the button back: pressing it is asking for another song.
    /// </summary>
    public static int? NextPressed(int count, int current, AudioRepeatMode repeat)
    {
        if (Next(count, current) is { } next)
            return next;

        return repeat == AudioRepeatMode.All && count > 0 ? 0 : null;
    }

    /// <summary>The mode the repeat button goes to after <paramref name="mode"/>.</summary>
    public static AudioRepeatMode Cycle(AudioRepeatMode mode) => mode switch
    {
        AudioRepeatMode.Off => AudioRepeatMode.All,
        AudioRepeatMode.All => AudioRepeatMode.One,
        _ => AudioRepeatMode.Off,
    };

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
