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
            MessageBox.Show(
                $"Listo. La copia quedó en:\n\n{dialog.FileName}\n\n({size:0.0} MB)\n\n"
                + "Guardala fuera de esta computadora — en un pendrive o en la nube —, "
                + "porque si se rompe el disco se va con él.",
                "Copia guardada", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo guardar la copia en {Path}", dialog.FileName);
            MessageBox.Show(
                $"No se pudo guardar la copia:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(
                "Ese archivo no es una copia de EcclesiaCast.\n\n"
                + "Buscá el que termina en «.ecbackup», o el archivo «ecclesiacast.db».",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DatabaseBackup.IsFromANewerVersion(dialog.FileName, dbPath))
        {
            MessageBox.Show(
                "Esa copia la hizo una versión de EcclesiaCast más nueva que la que tenés instalada.\n\n"
                + "Actualizá el programa y volvé a intentarlo.",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var answer = MessageBox.Show(
            $"La copia tiene:\n\n"
            + $"    • {summary.Songs} canciones\n"
            + $"    • {summary.BibleVersions} versiones de la Biblia\n"
            + $"    • {summary.MediaItems} medios\n"
            + $"    • {summary.Playlists} playlists\n\n"
            + "Al restaurarla, TODO lo que tenés ahora (canciones, Biblias, temas, medios y playlists) "
            + "se reemplaza por lo que hay en la copia.\n\n"
            + "EcclesiaCast se va a cerrar y volver a abrir solo. ¿Seguimos?",
            "Restaurar la copia", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            DatabaseBackup.StageRestore(dialog.FileName, dbPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo preparar la restauración desde {Path}", dialog.FileName);
            MessageBox.Show(
                $"No se pudo leer la copia:\n\n{ex.Message}",
                "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(
                    "La copia quedó lista para restaurarse, pero no pude volver a abrir EcclesiaCast solo.\n\n"
                    + "Abrilo de nuevo a mano y la restauración se completa sola.",
                    "EcclesiaCast", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        Application.Current.Shutdown();
    }
}
