namespace EcclesiaCast.App.Services;

/// <summary>Opens a small text prompt dialog, with one field or two.</summary>
public interface ITextPrompt
{
    /// <summary>Returns the entered text, or null if the operator cancelled.</summary>
    string? Ask(string title, string message, string initialValue = "");

    /// <summary>
    /// Offers a few named options as buttons. Returns the index of the one
    /// picked, or -1 if the operator cancelled. The first is the suggested one.
    /// </summary>
    int Choose(string title, string message, IReadOnlyList<string> options);

    /// <summary>
    /// The same, with a box under the buttons that the operator can tick to
    /// say "do this for the rest too" (the next ones of a batch). Index -1
    /// when cancelled.
    /// </summary>
    (int Index, bool Repeat) ChooseAndRemember(
        string title, string message, IReadOnlyList<string> options, string? rememberLabel);

    /// <summary>
    /// Asks for two values in the same window (a name and its abbreviation,
    /// say). Null if the operator cancelled.
    /// </summary>
    (string First, string Second)? AskTwo(
        string title, string firstLabel, string firstValue, string secondLabel, string secondValue);
}
