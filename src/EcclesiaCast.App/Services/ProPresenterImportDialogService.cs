using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.Services;

public sealed class ProPresenterImportDialogService(ISongRepository songs) : IProPresenterImportDialog
{
    public string? Show()
    {
        var window = new ProPresenterImportWindow(songs)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? window.Summary : null;
    }
}
