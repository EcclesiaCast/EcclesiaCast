using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaQuickTextEditor : IQuickTextEditor
{
    public string? Edit(string text, string? title = null, string? message = null)
    {
        var window = new QuickTextWindow(text, title, message);
        Modal.Run(window);
        return window.Result;
    }
}
