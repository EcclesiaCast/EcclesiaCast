using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Stage;

namespace EcclesiaCast.App.Services;

public sealed class StageDesignerService : IStageDesigner
{
    public StageLayoutState? Edit(IReadOnlyList<StageLayout> layouts, string activeName)
    {
        var window = new StageDesignerWindow(layouts, activeName)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
