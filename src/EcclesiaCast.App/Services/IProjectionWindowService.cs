using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the fullscreen output window shown on the projection display.</summary>
public interface IProjectionWindowService
{
    bool IsOutputVisible { get; }

    /// <summary>Raised whenever the output window becomes visible or hidden.</summary>
    event EventHandler? VisibilityChanged;

    /// <summary>Raised when the projected video finishes without looping.</summary>
    event EventHandler? VideoEnded;

    /// <summary>Shows (or moves) the output window fullscreen on the given display.</summary>
    void EnsureVisible(DisplayInfo display);

    void HideOutput();
}
