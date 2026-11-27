using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Songs.Web;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaWebSongSearchDialog(SongSaver songSaver, ISongRepository songs, IShell shell)
    : IWebSongSearchDialog
{
    public IReadOnlyList<Song> Show()
    {
        // Without a browser, no search by a line of the lyrics is promised.
        var viewModel = new WebSongSearchViewModel(songSaver, songs, new WebSongClient())
        {
            PhraseSearchPromised = false,
        };

        Modal.Run(new WebSongSearchWindow(viewModel, shell));
        return viewModel.Imported;
    }
}
