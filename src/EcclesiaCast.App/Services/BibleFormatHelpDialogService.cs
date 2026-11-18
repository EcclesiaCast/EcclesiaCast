using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class BibleFormatHelpDialogService : IBibleFormatHelpDialog
{
    public bool Show()
    {
        var window = new BibleFormatHelpWindow { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        return window.ImportRequested;
    }
}
