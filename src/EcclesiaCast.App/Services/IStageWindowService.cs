using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the stage display window shown to the musicians.</summary>
public interface IStageWindowService
{
    bool IsVisible { get; }

    /// <summary>Raised when the stage window is shown or hidden.</summary>
    event EventHandler? VisibilityChanged;

    void ShowOn(DisplayInfo display, StageLayout layout);

    void Hide();

    /// <summary>Draws another layout on the open window.</summary>
    void ApplyLayout(StageLayout layout);

    /// <summary>Restarts the "how long the service has been running" timer.</summary>
    void ResetTimer();
}
