using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;

namespace EcclesiaCast.App.Services;

public sealed class TrashDialogService(ISongRepository songs, IThemeRepository themes) : ITrashDialog
{
    public void Show() =>
        new TrashWindow(songs, themes) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
}
