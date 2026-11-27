using System.Diagnostics;
using System.Windows;
using EcclesiaCast.App.Platform;

namespace EcclesiaCast.App.Services;

/// <summary>
/// The backups of the WPF program: everything the operator sees is in the
/// shared <see cref="BackupDialog"/>; here is only how Windows opens the
/// program again and closes it.
/// </summary>
public sealed class BackupDialogService(string dbPath) : IBackupDialog
{
    private readonly BackupDialog _backup = new(
        dbPath,
        new WpfDialogs(),
        relaunch: () =>
        {
            if (Environment.ProcessPath is { } exe)
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        },
        beQuiet: () => FeedbackCoordinator.Quiet = true,
        shutdown: () => Application.Current.Shutdown());

    public void SaveBackup() => _backup.SaveBackup();

    public void RestoreBackup() => _backup.RestoreBackup();
}
