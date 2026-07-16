using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the fullscreen output window shown on the projection display.</summary>
public interface IProjectionWindowService
{
    bool IsOutputVisible { get; }

    /// <summary>Shows (or moves) the output window fullscreen on the given display.</summary>
    void EnsureVisible(DisplayInfo display);

    void HideOutput();
}
