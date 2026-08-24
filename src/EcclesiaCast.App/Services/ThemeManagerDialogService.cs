using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.Services;

public sealed class ThemeManagerDialogService(
    IThemeRepository themes, ISettingsStore settings, ISongRepository songs)
    : IThemeManagerDialog
{
    public bool Show()
    {
        var viewModel = new ThemeManagerViewModel(themes, settings, songs);
        var window = new ThemeManagerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return viewModel.ChangesMade;
    }
}
