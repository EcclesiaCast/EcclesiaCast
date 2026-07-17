namespace EcclesiaCast.App.Services;

/// <summary>Opens a small single-field text prompt dialog.</summary>
public interface ITextPrompt
{
    /// <summary>Returns the entered text, or null if the operator cancelled.</summary>
    string? Ask(string title, string message, string initialValue = "");
}
