using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

public sealed class WebSongSearchDialogService(ISongRepository songs) : IWebSongSearchDialog
{
    public IReadOnlyList<Song> Show()
    {
        var window = new WebSongSearchWindow(songs)
        {
            Owner = Application.Current.MainWindow,
        };

        window.ShowDialog();
        return window.Imported;
    }
}
