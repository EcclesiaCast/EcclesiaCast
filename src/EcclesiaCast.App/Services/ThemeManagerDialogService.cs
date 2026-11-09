using EcclesiaCast.App.ViewModels;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;

namespace EcclesiaCast.App.Services;

public sealed class ThemeManagerDialogService(
    IThemeRepository themes, ISettingsStore settings, ISongRepository songs)
    : IThemeManagerDialog
{
    public bool Show() => Open(null);

    public bool ShowForSong(Song song) => Open(song);

    private bool Open(Song? song)
    {
        var viewModel = new ThemeManagerViewModel(themes, settings, songs, song);
        var window = new ThemeManagerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return viewModel.ChangesMade;
    }
}
