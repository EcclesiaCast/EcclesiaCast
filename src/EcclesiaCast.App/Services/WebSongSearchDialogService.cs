using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class WebSongSearchDialogService(SongSaver songSaver, ISongRepository songs) : IWebSongSearchDialog
{
    public IReadOnlyList<Song> Show()
    {
        var viewModel = new WebSongSearchViewModel(songSaver, songs, new WebSongClient());
        var window = new WebSongSearchWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        window.ShowDialog();
        return viewModel.Imported;
    }
}
