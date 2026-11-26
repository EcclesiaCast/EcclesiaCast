namespace EcclesiaCast.Presentation.Platform;

public enum PlatformSystem { Windows, Mac }

/// <summary>The functions that do not exist on every system.</summary>
public enum Feature
{
    /// <summary>Showing another program's window on the output.</summary>
    WindowCapture,

    /// <summary>The web page panel.</summary>
    WebPanel,

    /// <summary>A YouTube live stream on the output.</summary>
    YouTubeLive,

    /// <summary>Bringing in a PowerPoint deck.</summary>
    PowerPoint,

    /// <summary>What is playing on the computer, and the media keys.</summary>
    NowPlaying,

    /// <summary>Seguir la canción: moving the slides by listening.</summary>
    FollowSong,

    /// <summary>The spelling checker of the editors.</summary>
    SpellCheck,

    /// <summary>«⧉ En una ventana»: the output in an ordinary window.</summary>
    FloatingOutput,
}

/// <summary>
/// Which system this is and whether a function exists here, so a screen can
/// hide the button or explain why it is not there. A function that is missing
/// never fails in silence.
/// </summary>
public interface IPlatformInfo
{
    PlatformSystem System { get; }

    bool IsAvailable(Feature feature);
}
