using System.Diagnostics;
using System.IO;
using System.Windows;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The operator's side of backups. Until now the answer to "how do I not lose
/// the songs" was "copy this file from a folder you have never opened", which
/// is not an answer for a volunteer.
/// </summary>
public sealed class BackupDialogService(string dbPath) : IBackupDialog
{
    public void SaveBackup()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Guardar una copia de la biblioteca",
            Filter = "Copia de EcclesiaCast (*.ecbackup)|*.ecbackup|Todos los archivos|*.*",
            FileName = DatabaseBackup.SuggestedFileName(DateTimeOffset.Now),
            AddExtension = true,
            DefaultExt = ".ecbackup",
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            DatabaseBackup.Create(dbPath, dialog.FileName);
            var size = new FileInfo(dialog.FileName).Length / (1024d * 1024d);

            Log.Information("Copia de seguridad guardada en {Path}", dialog.FileName);
            Notice.Show(
                $"La copia quedó en:\n\n{dialog.FileName}\n\n({size:0.0} MB)\n\n"
                + "Guardala fuera de esta computadora — en un pendrive o en la nube —, "
                + "porque si se rompe el disco se va con él.",
                "Copia guardada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo guardar la copia en {Path}", dialog.FileName);
            Notice.Show(ex.Message, "No se pudo guardar la copia", NoticeKind.Error);
        }
    }

    public void RestoreBackup()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Restaurar desde una copia",
            Filter = "Copia de EcclesiaCast (*.ecbackup;*.db)|*.ecbackup;*.db|Todos los archivos|*.*",
        };

        if (dialog.ShowDialog() != true)
            return;

        var summary = DatabaseBackup.Inspect(dialog.FileName);
        if (summary is null)
        {
            Notice.Show(
                "Buscá el que termina en «.ecbackup», o el archivo «ecclesiacast.db».",
                "Ese archivo no es una copia de EcclesiaCast",
                NoticeKind.Warning);
            return;
        }

        if (DatabaseBackup.IsFromANewerVersion(dialog.FileName, dbPath))
        {
            Notice.Show(
                "Esa copia la hizo una versión de EcclesiaCast más nueva que la que tenés instalada.\n\n"
                + "Actualizá el programa y volvé a intentarlo.",
                kind: NoticeKind.Warning);
            return;
        }

        var restoreIt = Notice.Confirm(
            $"La copia tiene:\n\n"
            + $"    • {summary.Songs} canciones\n"
            + $"    • {summary.BibleVersions} versiones de la Biblia\n"
            + $"    • {summary.MediaItems} medios\n"
            + $"    • {summary.Playlists} playlists\n\n"
            + "Al restaurarla, TODO lo que tenés ahora (canciones, Biblias, temas, medios y playlists) "
            + "se reemplaza por lo que hay en la copia.\n\n"
            + "EcclesiaCast se va a cerrar y volver a abrir solo.",
            "Restaurar", "Ahora no", "Restaurar la copia", destructive: true);

        if (!restoreIt)
            return;

        try
        {
            DatabaseBackup.StageRestore(dialog.FileName, dbPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo preparar la restauración desde {Path}", dialog.FileName);
            Notice.Show(ex.Message, "No se pudo leer la copia", NoticeKind.Error);
            return;
        }

        Log.Information("Restauración preparada desde {Path}; reiniciando", dialog.FileName);
        Restart();
    }

    /// <summary>
    /// The library is swapped in at startup, before anything opens it, so the
    /// program has to go around again for the restore to take effect.
    /// </summary>
    private static void Restart()
    {
        var exe = Environment.ProcessPath;
        if (exe is not null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "No se pudo relanzar EcclesiaCast; queda por cuenta del operador");
                Notice.Show(
                    "Abrilo de nuevo a mano y la restauración se completa sola.",
                    "La copia quedó lista, pero no pude volver a abrir EcclesiaCast solo");
            }
        }

        FeedbackCoordinator.Quiet = true;
        Application.Current.Shutdown();
    }
}
