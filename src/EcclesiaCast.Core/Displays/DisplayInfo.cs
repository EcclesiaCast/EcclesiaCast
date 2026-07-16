namespace EcclesiaCast.Core.Displays;

/// <summary>
/// A physical display attached to the system. Coordinates and size are in
/// physical pixels (virtual-screen space), as reported by the OS.
/// </summary>
public sealed record DisplayInfo(
    string DeviceName,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary);
