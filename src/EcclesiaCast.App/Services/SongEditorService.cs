using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

public sealed class SongEditorService(IThemeRepository themes) : ISongEditor
{
    public Song? Edit(Song? existing)
    {
        var window = new SongEditorWindow(existing, themes.GetAll())
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
