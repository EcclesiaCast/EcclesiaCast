using System.IO;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Presentation.Platform;
using Serilog;

namespace EcclesiaCast.Platform.Windows;

/// <summary>The installer's answers, which on Windows it leaves in the current user's registry.</summary>
public sealed class WindowsInstallerSettings : IInstallerSettings
{
    public string? ReadLowPerformanceChoice()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(LowPerformance.InstallerRegistryKey);
            return key?.GetValue(LowPerformance.InstallerValueName)?.ToString();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Debug(ex, "No se pudo leer la respuesta del instalador");
            return null;
        }
    }
}
