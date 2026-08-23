using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>Owns the stage display window shown to the musicians.</summary>
public interface IStageWindowService
{
    bool IsVisible { get; }

    /// <summary>Raised when the stage window is shown or hidden.</summary>
    event EventHandler? VisibilityChanged;

    void ShowOn(DisplayInfo display, StageOptions options);

    void Hide();

    /// <summary>Applies changed options to the open window.</summary>
    void ApplyOptions(StageOptions options);

    /// <summary>Restarts the "how long the service has been running" timer.</summary>
    void ResetTimer();
}
