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

    /// <summary>Runs one action; <paramref name="index"/> is used by the ones that take a target.</summary>
    void Execute(string action, int? index);
}
