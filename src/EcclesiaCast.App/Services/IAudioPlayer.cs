namespace EcclesiaCast.App.Services;

public enum AudioPlayerState
{
    /// <summary>Nothing loaded, or the track reached its end.</summary>
    Stopped,
    Playing,
    Paused,
}

/// <summary>
/// The player of the Música tab: one sound file at a time, on its own, apart
/// from the one that plays the video behind the slides (their volumes, pauses
/// and ends never touch each other).
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>False when the video engine did not start: there is no sound then.</summary>
    bool IsAvailable { get; }

    AudioPlayerState State { get; }

    TimeSpan Position { get; }

    /// <summary>Zero while the file is still being opened.</summary>
    TimeSpan Duration { get; }

    /// <summary>Its own volume, 0 to 100.</summary>
    int Volume { get; set; }

    /// <summary>Starts this file from the beginning, replacing whatever was playing.</summary>
    void Play(string path);

    void Pause();

    void Resume();

    void Stop();

    void SeekTo(TimeSpan position);

    /// <summary>The file played to its end. Raised on the UI thread, and never for a track already replaced.</summary>
    event EventHandler? Ended;

    /// <summary>The engine could not play the file. Raised on the UI thread.</summary>
    event EventHandler? Failed;
}
