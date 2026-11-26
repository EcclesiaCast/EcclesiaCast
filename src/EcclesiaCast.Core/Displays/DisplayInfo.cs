namespace EcclesiaCast.Core.Displays;

/// <summary>
/// A physical display attached to the system. Coordinates and size are in
/// physical pixels (virtual-screen space), as reported by the OS.
/// </summary>
/// <param name="Scale">
/// Pixels per point of that screen: 1 on a common monitor, 2 on a Mac's
/// Retina screen. A window is sized in points, so it is the size in pixels
/// over this. Windows (WPF) leaves it at 1: its windows are placed in pixels.
/// </param>
public sealed record DisplayInfo(
    string DeviceName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary,
    double Scale = 1);
