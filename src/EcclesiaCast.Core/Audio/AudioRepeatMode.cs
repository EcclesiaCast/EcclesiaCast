namespace EcclesiaCast.Core.Audio;

/// <summary>The repeat button of the Música tab's player.</summary>
public enum AudioRepeatMode
{
    /// <summary>The list plays once and the music stops after the last track.</summary>
    Off,

    /// <summary>After the last track the list starts again from the first.</summary>
    All,

    /// <summary>The same track over and over (a pad under a song, a loop of background music).</summary>
    One,
}
