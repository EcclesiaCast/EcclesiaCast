namespace EcclesiaCast.App.Remote;

/// <summary>One slide as the phone sees it.</summary>
public sealed record RemoteSlide(int Index, string Label, string Preview, bool IsLive);

/// <summary>One playlist entry as the phone sees it.</summary>
public sealed record RemotePlaylistItem(int Index, string Caption, string Kind);

/// <summary>
/// Everything the phone needs to draw itself. Sent as JSON on every poll, so
/// it stays small: slide previews are trimmed and nothing binary travels.
/// </summary>
public sealed record RemoteState(
    bool IsProjecting,
    string OutputState,
    string SlideLabel,
    string LiveText,
    string NextText,
    string Status,
    IReadOnlyList<RemoteSlide> Slides,
    IReadOnlyList<RemotePlaylistItem> Playlist,
    string SongTitle);

/// <summary>
/// What the phone is allowed to do. Implemented by the operator's view model;
/// the server marshals every call onto the UI thread.
/// </summary>
public interface IRemoteHost
{
    RemoteState GetState();

    /// <summary>The words on the projector, for the broadcast page.</summary>
    RemoteOutput GetOutput();

    /// <summary>Runs one action; <paramref name="index"/> is used by the ones that take a target.</summary>
    void Execute(string action, int? index);
}

/// <summary>
/// What a broadcast page needs to draw the words the congregation is seeing,
/// with no operator interface around them: the text, the way the theme
/// styles it, and the announcements that ride over everything.
///
/// It carries style rather than a picture because that is what a stream
/// wants — clean text over transparency, to sit on top of the camera.
/// </summary>
public sealed record RemoteOutput(
    bool ShowText,
    string MainText,
    string SecondaryText,
    string Caption,
    string Overlay,
    string Countdown,
    string CountdownHeading,
    string FontFamily,
    double FontSize,
    bool Bold,
    bool Italic,
    string Color,
    string OutlineColor,
    double OutlineWidth,
    string AlignH,
    string AlignV);
