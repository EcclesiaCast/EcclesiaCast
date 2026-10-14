namespace EcclesiaCast.App.Remote;

/// <summary>
/// One slide as the phone sees it. <paramref name="MediaId"/> is set on the
/// cards of a presentation: the phone draws the page's poster (asking for it
/// by this id) instead of the text.
/// </summary>
public sealed record RemoteSlide(int Index, string Label, string Preview, bool IsLive, int? MediaId = null);

/// <summary>A tab marked as a presentation, which the phone can open on the computer.</summary>
public sealed record RemotePresentation(int Index, string Name, int Pages);

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
    string SongTitle,
    IReadOnlyList<RemotePresentation>? Presentations = null,
    int? LiveMediaId = null,
    int? NextMediaId = null);

/// <summary>
/// What the phone is allowed to do. Implemented by the operator's view model;
/// the server marshals every call onto the UI thread.
/// </summary>
public interface IRemoteHost
{
    RemoteState GetState();

    /// <summary>
    /// The small poster of a media the library knows, for the pages of a
    /// presentation on the phone; null when there is none. It is the poster
    /// only (never the full-size picture), and the path is never taken from
    /// the request: the phone only names the media by its id.
    /// </summary>
    string? GetThumbnailPath(int mediaId);

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
/// wants — clean text over transparency, to sit on top of the camera. The
/// marker-pen highlight goes as [start, length] pairs over each text.
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
    string AlignV,
    IReadOnlyList<int[]> MainMarks,
    IReadOnlyList<int[]> SecondaryMarks);
