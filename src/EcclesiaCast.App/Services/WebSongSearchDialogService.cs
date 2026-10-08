using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

public sealed class WebSongSearchDialogService(SongSaver songSaver, ISongRepository songs) : IWebSongSearchDialog
{
    public IReadOnlyList<Song> Show()
    {
        var window = new WebSongSearchWindow(songSaver, songs)
        {
            Owner = Application.Current.MainWindow,
        };

        window.ShowDialog();
        return window.Imported;
    }
}
