using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class TrashDialogService(ISongRepository songs, IThemeRepository themes) : ITrashDialog
{
    public void Show() =>
        new TrashWindow(new TrashViewModel(songs, themes)) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
}
