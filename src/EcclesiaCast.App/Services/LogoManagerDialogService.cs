using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class LogoManagerDialogService(
    ILogoRepository logos, IThumbnailer thumbnails, IColorPicker colorPicker,
    IFontCatalog fonts, IUiTimerFactory timers) : ILogoManagerDialog
{
    public bool Show()
    {
        var viewModel = new LogoManagerViewModel(logos, thumbnails, colorPicker, fonts, timers);
        var window = new LogoManagerWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
        return viewModel.ChangesMade;
    }
}
