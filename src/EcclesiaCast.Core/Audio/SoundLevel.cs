namespace EcclesiaCast.Core.Audio;

/// <summary>
/// How loud a player sounds, worked out by the program instead of by the
/// sound output of LibVLC. The usual output keeps ONE volume and ONE mute for
/// the whole program (a Windows audio session shared by every player), so
/// muting the blurred copy that fills the edges silenced the video itself.
/// That shared volume carries the general «Volumen» bar. A video's own volume
/// is turned down with the pre-amplifier and bands of its equalizer (which
/// belong to that player only), and is heard about 1.2 s later: the equalizer
/// works on sound that LibVLC prepares that far ahead. That is why the music
/// list, whose bar moves while it sounds, does not use it: it plays through
/// DirectSound, where each player has a volume of its own that acts at once.
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

    /// <summary>How far down the «Volumen» bar goes before it reaches silence (dB).</summary>
    public const double MasterRangeDb = 45;

    /// <summary>Under this fraction of the bar the sound fades out to silence instead of dropping on a step.</summary>
    private const double MasterFadeFraction = 0.04;

    /// <summary>
    /// The volume LibVLC's shared session is given (0 to 100) for the position
    /// of the general «Volumen» bar. LibVLC's own law is the cube of the number
    /// (a step of 1 near the bottom is a jump of several dB), so the bar is
    /// spread evenly in decibels instead: every step of the bar is about the
    /// same change in loudness. Silence (0) is only reached at the very bottom
    /// of the bar; above it the number stays at 1 or more, so the sound is
    /// never cut, only turned down.
    /// </summary>
    public static int SessionVolume(int master)
    {
        var fraction = Math.Clamp(master, 0, 100) / 100.0;
        if (fraction <= 0)
            return 0;
        if (fraction >= 1)
            return 100;

        var db = -MasterRangeDb * (1 - fraction);
        var volume = 100 * Math.Pow(10, db / 60);
        // The last few percent of the bar glide down to the lowest audible step.
        if (fraction < MasterFadeFraction)
            volume *= fraction / MasterFadeFraction;
        return Math.Clamp((int)Math.Round(volume), 1, 100);
    }

    /// <summary>
    /// The same general volume as Windows counts it for the program's audio
    /// session (0 to 1): the cube of <see cref="SessionVolume"/>, which is
    /// what LibVLC writes there when that number is given to one of its players.
    /// </summary>
    public static float SessionScalar(int master) => (float)Math.Pow(SessionVolume(master) / 100.0, 3);

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
