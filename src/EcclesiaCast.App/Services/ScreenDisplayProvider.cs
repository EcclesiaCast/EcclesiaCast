using EcclesiaCast.Core.Displays;

namespace EcclesiaCast.App.Services;

/// <summary>Enumerates displays using the Windows Forms Screen API.</summary>
public sealed class ScreenDisplayProvider : IDisplayProvider
{
    public IReadOnlyList<DisplayInfo> GetDisplays() =>
        System.Windows.Forms.Screen.AllScreens
            .Select(s => new DisplayInfo(
                s.DeviceName,
                s.Bounds.X,
                s.Bounds.Y,
                s.Bounds.Width,
                s.Bounds.Height,
                s.Primary))
            .ToList();
}
