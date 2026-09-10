namespace EcclesiaCast.App.Services;

/// <summary>Saving the whole library to a file, and putting one back.</summary>
public interface IBackupDialog
{
    void SaveBackup();

    void RestoreBackup();
}
