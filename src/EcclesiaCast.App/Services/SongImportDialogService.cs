using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class SongImportDialogService(SongSaver songSaver, IPlatformPaths paths) : ISongImportDialog
{
    public string? Show()
    {
        var viewModel = new SongImportViewModel(songSaver, paths);
        var window = new SongImportWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? viewModel.Summary : null;
    }
}
