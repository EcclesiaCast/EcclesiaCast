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

    public int Choose(string title, string message, IReadOnlyList<string> options) =>
        ChooseAndRemember(title, message, options, rememberLabel: null).Index;

    public (int Index, bool Repeat) ChooseAndRemember(
        string title, string message, IReadOnlyList<string> options, string? rememberLabel)
    {
        // Over whichever window the operator is looking at: a question asked
        // from inside an import window must not hide behind the main one.
        var app = System.Windows.Application.Current;
        var owner = app.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
                    ?? app.MainWindow;
        var window = new ChoiceWindow(title, message, options, rememberLabel) { Owner = owner };
        return window.ShowDialog() == true ? (window.Chosen, window.Remember) : (-1, false);
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
