using EcclesiaCast.App.Views;
using EcclesiaCast.Presentation.Remote;

namespace EcclesiaCast.App.Services;

public sealed class RemoteControlDialogService : IRemoteControlDialog
{
    public bool Show(RemoteControlServer server)
    {
        var window = new RemoteControlWindow(server)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
        return window.StopRequested;
    }
}
