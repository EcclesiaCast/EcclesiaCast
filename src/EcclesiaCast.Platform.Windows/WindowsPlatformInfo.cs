using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows;

/// <summary>Every function of EcclesiaCast exists on Windows.</summary>
public sealed class WindowsPlatformInfo : IPlatformInfo
{
    public PlatformSystem System => PlatformSystem.Windows;

    public bool IsAvailable(Feature feature) => true;
}
