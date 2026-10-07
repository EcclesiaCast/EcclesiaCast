namespace EcclesiaCast.App.Services;

/// <summary>Opens a small text prompt dialog, with one field or two.</summary>
public interface ITextPrompt
{
    /// <summary>Returns the entered text, or null if the operator cancelled.</summary>
    string? Ask(string title, string message, string initialValue = "");

    /// <summary>
    /// Asks for two values in the same window (a name and its abbreviation,
    /// say). Null if the operator cancelled.
    /// </summary>
    (string First, string Second)? AskTwo(
        string title, string firstLabel, string firstValue, string secondLabel, string secondValue);
}
