using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class QuickTextEditorService : IQuickTextEditor
{
    public string? Edit(string text)
    {
        var window = new QuickTextWindow(text)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
