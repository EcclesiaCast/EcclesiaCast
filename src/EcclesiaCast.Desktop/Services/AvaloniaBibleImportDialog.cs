using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaBibleImportDialog : IBibleImportDialog
{
    public BibleImportMetadata? Prompt(string suggestedName)
    {
        var window = new BibleImportWindow(suggestedName);
        Modal.Run(window);
        return window.Result;
    }
}
