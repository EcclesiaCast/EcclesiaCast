using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Presentation.Remote;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class BroadcastDialogService(ISettingsStore settings, IColorPicker colorPicker) : IBroadcastDialog
{
    public void ShowPages(RemoteControlServer server, Action<string> obsPinChanged) =>
        new BroadcastWindow(server, owner => ShowStylesOver(owner), obsPinChanged)
        {
            Owner = Application.Current.MainWindow,
        }.ShowDialog();

    public void ShowStyles() => ShowStylesOver(Application.Current.MainWindow);

    private void ShowStylesOver(Window owner) =>
        new BroadcastStyleWindow(new BroadcastStyleViewModel(settings, colorPicker)) { Owner = owner }.ShowDialog();
}
