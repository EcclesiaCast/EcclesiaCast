namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Hands something to the system to open with what the operator has for it.
/// A failure (no browser, a path that went away) comes back as an exception,
/// for the caller to explain.
/// </summary>
public interface IShell
{
    /// <summary>Opens a web address in the browser.</summary>
    void OpenUrl(string url);

    /// <summary>Opens a folder in the system's file window.</summary>
    void OpenFolder(string folder);

    /// <summary>Opens the folder of <paramref name="file"/> with the file selected.</summary>
    void ShowInFolder(string file);
}
