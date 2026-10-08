using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class QuickTextEditorService : IQuickTextEditor
{
    public string? Edit(string text, string? title = null, string? message = null)
    {
        var window = new QuickTextWindow(text, title, message)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
