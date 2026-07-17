using EcclesiaCast.App.Views;

namespace EcclesiaCast.App.Services;

public sealed class TextPromptService : ITextPrompt
{
    public string? Ask(string title, string message, string initialValue = "")
    {
        var window = new TextPromptWindow(title, message, initialValue)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Value : null;
    }
}
