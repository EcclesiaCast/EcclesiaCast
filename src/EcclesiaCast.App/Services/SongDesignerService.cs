using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;

namespace EcclesiaCast.App.Services;

public sealed class SongDesignerService : ISongDesigner
{
    public bool Edit(Song song, SlideTheme resolvedTheme, int selectIndex)
    {
        var window = new SongDesignerWindow(song, resolvedTheme, selectIndex)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return window.Saved;
    }
}
