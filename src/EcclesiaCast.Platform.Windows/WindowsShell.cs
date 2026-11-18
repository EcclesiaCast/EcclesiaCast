using System.Diagnostics;
using EcclesiaCast.Presentation.Platform;

namespace EcclesiaCast.Platform.Windows;

/// <summary>Opens things the way Windows' Explorer does.</summary>
public sealed class WindowsShell : IShell
{
    public void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    public void OpenFolder(string folder) =>
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });

    public void ShowInFolder(string file) =>
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{file}\"",
            UseShellExecute = true,
        });
}
