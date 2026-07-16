using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the fullscreen output window shown on the projection display.</summary>
public interface IProjectionWindowService
{
    bool IsOutputVisible { get; }

    /// <summary>Shows the test signal fullscreen on the given display.</summary>
    void ShowTest(DisplayInfo display);

    void HideOutput();
}
