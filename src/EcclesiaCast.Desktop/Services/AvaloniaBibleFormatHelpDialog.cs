using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaBibleFormatHelpDialog(IDialogs dialogs, IClipboardText clipboard, IPlatformInfo platform)
    : IBibleFormatHelpDialog
{
    public bool Show()
    {
        var window = new BibleFormatHelpWindow(new BibleFormatHelpViewModel(dialogs, clipboard, platform.System));
        Modal.Run(window);
        return window.ImportRequested;
    }
}
