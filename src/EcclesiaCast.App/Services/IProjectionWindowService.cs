using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the fullscreen output window shown on the projection display.</summary>
public interface IProjectionWindowService
{
    bool IsOutputVisible { get; }

    /// <summary>Where the projected video is; <see cref="PlaybackState.None"/> when nothing plays.</summary>
    PlaybackState Playback { get; }

    void TogglePlayPause();

    void SeekTo(TimeSpan position);

    /// <summary>Jumps forward (positive) or back (negative) within the video.</summary>
    void Skip(TimeSpan delta);

    /// <summary>Raised whenever the output window becomes visible or hidden.</summary>
    event EventHandler? VisibilityChanged;

    /// <summary>Raised when the projected video finishes without looping.</summary>
    event EventHandler? VideoEnded;

    /// <summary>Shows (or moves) the output window fullscreen on the given display.</summary>
    void EnsureVisible(DisplayInfo display);

    void HideOutput();
}
