using System.Windows;
using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class SongImportDialogService(SongSaver songSaver) : ISongImportDialog
{
    public string? Show()
    {
        var window = new SongImportWindow(songSaver)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? window.Summary : null;
    }
}
