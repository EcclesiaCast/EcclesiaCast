using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Which screen holds the operator panel. A fullscreen window sent there
/// covers the panel it was started from.
/// </summary>
public interface IOperatorScreen
{
    /// <summary>True when that display is the one holding the operator window.</summary>
    bool Holds(DisplayInfo display);
}
