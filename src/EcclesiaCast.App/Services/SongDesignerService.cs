using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class SongDesignerService(IColorPicker colorPicker, IFontCatalog fonts) : ISongDesigner
{
    public bool Edit(Song song, SlideTheme resolvedTheme, int selectIndex)
    {
        var viewModel = new SongDesignerViewModel(song, resolvedTheme, colorPicker, fonts);
        var window = new SongDesignerWindow(viewModel, selectIndex)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return viewModel.Saved;
    }
}
