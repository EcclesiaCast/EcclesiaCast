using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.Remote;

namespace EcclesiaCast.App.Services;

public sealed class BroadcastDialogService(ISettingsStore settings) : IBroadcastDialog
{
    public void ShowPages(RemoteControlServer server) =>
        new BroadcastWindow(server, settings) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();

    public void ShowStyles() =>
        new BroadcastStyleWindow(settings) { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
}
