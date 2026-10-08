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

    public int Choose(string title, string message, IReadOnlyList<string> options)
    {
        var window = new ChoiceWindow(title, message, options)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Chosen : -1;
    }

    public (string First, string Second)? AskTwo(
        string title, string firstLabel, string firstValue, string secondLabel, string secondValue)
    {
        var window = new TextPromptWindow(title, firstLabel, firstValue)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.AddSecondField(secondLabel, secondValue);
        return window.ShowDialog() == true ? (window.Value, window.SecondValue) : null;
    }
}
