using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaGlobalSearchDialog(IUiTimerFactory timers) : IGlobalSearchDialog
{
    public SearchHit? Show(Func<string, IReadOnlyList<SearchHit>> search, string initialQuery)
    {
        var window = new GlobalSearchWindow(search, initialQuery, timers);
        Modal.Run(window);
        // Only «Cargar», Enter and the double click leave a result.
        return window.Result;
    }
}
