using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class BibleImportDialogService : IBibleImportDialog
{
    public BibleImportMetadata? Prompt(string suggestedName)
    {
        var window = new BibleImportWindow(suggestedName)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true
            ? new BibleImportMetadata(window.ResultName, window.ResultAbbreviation, window.ResultLanguage)
            : null;
    }
}
