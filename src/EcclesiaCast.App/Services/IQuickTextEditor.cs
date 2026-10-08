namespace EcclesiaCast.App.Services;

/// <summary>Quick text-only edit: a single slide, or the stored text of a verse.</summary>
public interface IQuickTextEditor
{
    /// <summary>
    /// Returns the edited text, or null if cancelled. The window's title and
    /// the sentence above the box can be replaced when the text is not a
    /// slide's (a verse stored in a Bible).
    /// </summary>
    string? Edit(string text, string? title = null, string? message = null);
}
