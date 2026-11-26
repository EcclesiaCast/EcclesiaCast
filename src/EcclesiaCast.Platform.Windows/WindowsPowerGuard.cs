using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows;

/// <summary>
/// On Windows, what the program always did: at start-up it asks not to be
/// run in «efficiency mode» (<see cref="PowerThrottling"/>), for the whole
/// run. Projecting asks for nothing more, as before.
/// </summary>
public sealed class WindowsPowerGuard : IPowerGuard
{
    public void Start() => PowerThrottling.OptOut();

    public void BeginProjecting()
    {
    }

    public void EndProjecting()
    {
    }
}
