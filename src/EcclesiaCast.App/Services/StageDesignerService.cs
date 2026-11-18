using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.App.Services;

public sealed class StageDesignerService(IColorPicker colorPicker, IFontCatalog fonts) : IStageDesigner
{
    public StageLayoutState? Edit(IReadOnlyList<StageLayout> layouts, string activeName)
    {
        // The prompts open over the designer, so the view model asks through the window.
        StageDesignerWindow? window = null;
        var viewModel = new StageDesignerViewModel(
            layouts, activeName, (title, message, initial) => window!.Ask(title, message, initial),
            colorPicker, fonts);
        window = new StageDesignerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
