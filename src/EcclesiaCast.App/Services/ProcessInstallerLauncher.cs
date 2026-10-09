using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using EcclesiaCast.Core.Updates;

namespace EcclesiaCast.App.Services;

/// <summary>
/// Starts the installer of an update. It asks for administrator permission
/// from here ("runas") and not from the installer: if the operator says no,
/// Windows tells this program, which is still open to say so, instead of an
/// installer that nobody is looking at failing after the program has closed.
/// </summary>
public sealed class ProcessInstallerLauncher : IInstallerLauncher
{
    /// <summary>ERROR_CANCELLED: the operator refused the permission prompt.</summary>
    private const int PermissionRefused = 1223;

    public void Launch(string installerPath, string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo(installerPath, arguments)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty,
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == PermissionRefused)
        {
            throw new UpdateException(UpdateFailure.Launch,
                "Windows no dio el permiso para instalar la actualización, así que no se instaló nada. "
                + "Podés probar de nuevo y aceptar el permiso.", ex);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            throw new UpdateException(UpdateFailure.Launch,
                "No se pudo abrir el instalador de la actualización, así que no se instaló nada.", ex);
        }
    }
}
