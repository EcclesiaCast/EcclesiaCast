using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

/// <summary>The small questions, each in its window, waiting for the answer (WPF's TextPromptService).</summary>
public sealed class AvaloniaTextPrompt : ITextPrompt
{
    public string? Ask(string title, string message, string initialValue = "")
    {
        var window = new TextPromptWindow(title, message, initialValue);
        Modal.Run(window);
        return window.Accepted ? window.Value : null;
    }

    public int Choose(string title, string message, IReadOnlyList<string> options) =>
        ChooseAndRemember(title, message, options, rememberLabel: null).Index;

    public (int Index, bool Repeat) ChooseAndRemember(
        string title, string message, IReadOnlyList<string> options, string? rememberLabel)
    {
        var window = new ChoiceWindow(title, message, options, rememberLabel);
        Modal.Run(window);
        return window.Chosen < 0 ? (-1, false) : (window.Chosen, window.Remember);
    }

    public (string First, string Second)? AskTwo(
        string title, string firstLabel, string firstValue, string secondLabel, string secondValue)
    {
        var window = new TextPromptWindow(title, firstLabel, firstValue);
        window.AddSecondField(secondLabel, secondValue);
        Modal.Run(window);
        return window.Accepted ? (window.Value, window.SecondValue) : null;
    }
}
