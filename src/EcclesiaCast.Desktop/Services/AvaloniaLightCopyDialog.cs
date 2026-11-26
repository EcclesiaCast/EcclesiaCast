using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaLightCopyDialog : ILightCopyDialog
{
    public LightCopyResult Run(string path, string name)
    {
        var copy = LightCopyViewModel.For(path, name);
        Modal.Run(new LightCopyWindow(copy));
        return copy.Result;
    }
}
