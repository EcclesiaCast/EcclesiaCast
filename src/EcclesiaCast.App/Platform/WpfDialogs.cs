using Microsoft.Win32;

namespace EcclesiaCast.App.Platform;

/// <summary>
/// Windows' own file dialogs, as the program always used them: shown without
/// an owner, so WPF puts them over the window that is active.
/// </summary>
public sealed class WpfDialogs : IDialogs
{
    public string? OpenFile(OpenFileRequest request)
    {
        var dialog = new OpenFileDialog
        {
            Title = request.Title,
            Filter = FilterText(request.Filters),
        };
        if (request.InitialDirectory is { } folder)
            dialog.InitialDirectory = folder;
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string>? OpenFiles(OpenFileRequest request)
    {
        var dialog = new OpenFileDialog
        {
            Title = request.Title,
            Filter = FilterText(request.Filters),
            Multiselect = true,
        };
        if (request.InitialDirectory is { } folder)
            dialog.InitialDirectory = folder;
        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(SaveFileRequest request)
    {
        var dialog = new SaveFileDialog
        {
            Title = request.Title,
            Filter = FilterText(request.Filters),
            AddExtension = request.AddExtension,
        };
        if (request.FileName is { } fileName)
            dialog.FileName = fileName;
        if (request.DefaultExtension is { } extension)
            dialog.DefaultExt = extension;
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    /// <summary>"Imágenes|*.jpg;*.png|Videos|*.mp4": the way Windows takes the list.</summary>
    internal static string FilterText(IReadOnlyList<FileFilter> filters) =>
        string.Join("|", filters.Select(f => f.Name + "|" + string.Join(";", f.Patterns)));
}
