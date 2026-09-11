using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class GlobalSearchDialogService : IGlobalSearchDialog
{
    public SearchHit? Show(Func<string, IReadOnlyList<SearchHit>> search, string initialQuery)
    {
        var window = new GlobalSearchWindow(search, initialQuery)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        return window.ShowDialog() == true ? window.Result : null;
    }
}
