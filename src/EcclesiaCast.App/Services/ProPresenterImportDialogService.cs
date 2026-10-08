using System.Windows;
using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class ProPresenterImportDialogService(SongSaver songSaver) : IProPresenterImportDialog
{
    public string? Show()
    {
        var window = new ProPresenterImportWindow(songSaver)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? window.Summary : null;
    }
}
