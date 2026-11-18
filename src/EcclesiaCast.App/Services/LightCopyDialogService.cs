using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class LightCopyDialogService : ILightCopyDialog
{
    public LightCopyResult Run(string path, string name)
    {
        var window = new LightCopyWindow(path, name)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (window.ShowDialog() == true)
            return LightCopyResult.Done;

        return window.Cancelled ? LightCopyResult.Cancelled : LightCopyResult.Failed;
    }
}
