using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.Presentation.Services;

/// <summary>
/// The operator's side of backups, the same on every system. Until now the
/// answer to "how do I not lose the songs" was "copy this file from a folder
/// you have never opened", which is not an answer for a volunteer.
///
/// What belongs to each system comes from outside: how the program is opened
/// again, how it stays quiet while it closes and how it is closed.
/// </summary>
/// <param name="relaunch">Opens the program again; throws when it cannot.</param>
/// <param name="beQuiet">No farewell question on the way out.</param>
public sealed class BackupDialog(
    string dbPath, IDialogs dialogs, Action relaunch, Action beQuiet, Action shutdown) : IBackupDialog
{
    public void SaveBackup()
    {
        var path = dialogs.SaveFile(new SaveFileRequest(
            "Guardar una copia de la biblioteca",
            [new FileFilter("Copia de EcclesiaCast (*.ecbackup)", ["*.ecbackup"]), AllFiles])
        {
            FileName = DatabaseBackup.SuggestedFileName(DateTimeOffset.Now),
            AddExtension = true,
            DefaultExtension = ".ecbackup",
        });

        if (path is null)
            return;

        try
        {
            DatabaseBackup.Create(dbPath, path);
            var size = new FileInfo(path).Length / (1024d * 1024d);

            Log.Information("Copia de seguridad guardada en {Path}", path);
            Notice.Show(
                $"La copia quedó en:\n\n{path}\n\n({size:0.0} MB)\n\n"
                + "Guardala fuera de esta computadora — en un pendrive o en la nube —, "
                + "porque si se rompe el disco se va con él.",
                "Copia guardada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo guardar la copia en {Path}", path);
            Notice.Show(ex.Message, "No se pudo guardar la copia", NoticeKind.Error);
        }
    }

    public void RestoreBackup()
    {
        var path = dialogs.OpenFile(new OpenFileRequest(
            "Restaurar desde una copia",
            [new FileFilter("Copia de EcclesiaCast (*.ecbackup;*.db)", ["*.ecbackup", "*.db"]), AllFiles]));

        if (path is null)
            return;

        var summary = DatabaseBackup.Inspect(path);
        if (summary is null)
        {
            Notice.Show(
                "Buscá el que termina en «.ecbackup», o el archivo «ecclesiacast.db».",
                "Ese archivo no es una copia de EcclesiaCast",
                NoticeKind.Warning);
            return;
        }

        if (DatabaseBackup.IsFromANewerVersion(path, dbPath))
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
            DatabaseBackup.StageRestore(path, dbPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo preparar la restauración desde {Path}", path);
            Notice.Show(ex.Message, "No se pudo leer la copia", NoticeKind.Error);
            return;
        }

        Log.Information("Restauración preparada desde {Path}; reiniciando", path);
        Restart();
    }

    private static FileFilter AllFiles => new("Todos los archivos", ["*.*"]);

    /// <summary>
    /// The library is swapped in at startup, before anything opens it, so the
    /// program has to go around again for the restore to take effect.
    /// </summary>
    private void Restart()
    {
        try
        {
            relaunch();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo relanzar EcclesiaCast; queda por cuenta del operador");
            Notice.Show(
                "Abrilo de nuevo a mano y la restauración se completa sola.",
                "La copia quedó lista, pero no pude volver a abrir EcclesiaCast solo");
        }

        beQuiet();
        shutdown();
    }
}
