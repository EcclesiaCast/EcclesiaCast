namespace EcclesiaCast.Core.Audio;

/// <summary>
/// How loud a player sounds, worked out by the program instead of by the
/// sound output of LibVLC. That output keeps ONE volume and ONE mute for the
/// whole program (a Windows audio session shared by every player), so muting
/// the blurred copy that fills the edges silenced the video itself, and the
/// music list's volume changed the video's. Each player now stays at full
/// volume on that shared session and is turned down on its own, with the
/// pre-amplifier and bands of its equalizer (which belong to that player only).
/// </summary>
public static class SoundLevel
{
    /// <summary>The most the pre-amplifier of LibVLC's equalizer takes away (dB).</summary>
    public const double PreampFloor = -20;

    /// <summary>The most each band of the equalizer takes away (dB).</summary>
    public const double BandFloor = -20;

    /// <summary>
    /// What the pre-amplifier has to give for the sound to come out as loud as
    /// without an equalizer: LibVLC's equalizer takes about 12 dB away on its
    /// own (its «Flat» preset carries +12). Measured on 8/11/2026: with the
    /// pre-amplifier at 0, 80 % came out 18 dB down instead of 6.
    /// </summary>
    public const double PreampUnity = 12;

    /// <summary>The quietest level reached before 0 % (which is silence).</summary>
    public const double Floor = PreampFloor - PreampUnity + BandFloor;

    /// <summary>
    /// The level for a volume from 0 to 100, in dB under full: 100 is 0 dB.
    /// It falls like a volume knob does (the same curve VLC uses: the cube of
    /// the fraction), so 50 % sounds about half. 0 is never asked for: it is
    /// played as silence.
    /// </summary>
    public static double ToDecibels(int volume)
    {
        var fraction = Math.Clamp(volume, 0, 100) / 100.0;
        if (fraction <= 0)
            return Floor;

        var db = 60 * Math.Log10(fraction);
        return Math.Clamp(db, Floor, 0);
    }

    /// <summary>
    /// The level split in what the pre-amplifier takes away and what every band
    /// takes away (the bands move together, so the sound keeps its colour).
    /// Null at full volume: then no equalizer is needed at all.
    /// </summary>
    public static (double Preamp, double Bands)? Split(int volume)
    {
        if (volume >= 100)
            return null;

        var db = ToDecibels(volume);
        var preamp = Math.Max(PreampUnity + db, PreampFloor);
        var bands = Math.Max(PreampUnity + db - preamp, BandFloor);
        return (preamp, bands);
    }
}
