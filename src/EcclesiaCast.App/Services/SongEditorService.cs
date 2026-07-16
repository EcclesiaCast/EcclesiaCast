using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

public sealed class SongEditorService : ISongEditor
{
    public Song? Edit(Song? existing)
    {
        var window = new SongEditorWindow(existing)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
