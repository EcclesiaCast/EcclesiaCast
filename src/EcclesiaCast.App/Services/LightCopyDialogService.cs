using EcclesiaCast.App.Views;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class LightCopyDialogService : ILightCopyDialog
{
    public LightCopyResult Run(string path, string name)
    {
        var copy = LightCopyViewModel.For(path, name);
        var window = new LightCopyWindow(copy)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        window.ShowDialog();
        return copy.Result;
    }
}
