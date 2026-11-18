namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// One line of the «Tipo» list of a file dialog: what the operator reads and
/// the patterns it lets through ("*.jpg", "*.png").
/// </summary>
public sealed record FileFilter(string Name, IReadOnlyList<string> Patterns);

/// <summary>What an «Abrir» dialog asks for.</summary>
public sealed record OpenFileRequest(string Title, IReadOnlyList<FileFilter> Filters);

/// <summary>What a «Guardar» dialog asks for.</summary>
public sealed record SaveFileRequest(string Title, IReadOnlyList<FileFilter> Filters)
{
    /// <summary>The name the dialog opens with.</summary>
    public string? FileName { get; init; }

    /// <summary>Added to a name typed without one (".json").</summary>
    public string? DefaultExtension { get; init; }

    /// <summary>Whether a name typed without extension gets <see cref="DefaultExtension"/>.</summary>
    public bool AddExtension { get; init; } = true;
}

/// <summary>
/// The system's own dialogs to open and save files and to choose a folder.
/// Each one waits for the operator and returns null when they cancel.
/// </summary>
public interface IDialogs
{
    /// <summary>One file, or null.</summary>
    string? OpenFile(OpenFileRequest request);

    /// <summary>One or more files, or null.</summary>
    IReadOnlyList<string>? OpenFiles(OpenFileRequest request);

    /// <summary>Where to save, or null.</summary>
    string? SaveFile(SaveFileRequest request);

    /// <summary>A folder, or null.</summary>
    string? PickFolder(string title);
}
