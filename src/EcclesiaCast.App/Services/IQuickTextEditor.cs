namespace EcclesiaCast.App.Services;

/// <summary>Quick text-only edit of a single slide.</summary>
public interface IQuickTextEditor
{
    /// <summary>Returns the edited text, or null if cancelled.</summary>
    string? Edit(string text);
}
