using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EcclesiaCast.Desktop.Views;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The system's own dialogs to open and save files and to choose a folder,
/// through Avalonia's <c>StorageProvider</c> (the WPF one is
/// <c>WpfDialogs</c>). Avalonia answers later and the view models read the
/// answer on the next line, so the screen's thread waits in a nested loop, as
/// <see cref="Modal"/> does. Like WPF's, they open where the system was last,
/// unless the request names a folder.
/// </summary>
public sealed class AvaloniaDialogs : IDialogs
{
    public string? OpenFile(OpenFileRequest request) =>
        Ask(storage => storage.OpenFilePickerAsync(StartingIn(storage, request, OpenOptions(request, several: false))))
            ?.Select(f => f.TryGetLocalPath())
            .FirstOrDefault(p => p is not null);

    public IReadOnlyList<string>? OpenFiles(OpenFileRequest request)
    {
        var paths = Ask(storage => storage.OpenFilePickerAsync(StartingIn(storage, request, OpenOptions(request, several: true))))
            ?.Select(f => f.TryGetLocalPath())
            .OfType<string>()
            .ToList();
        return paths is { Count: > 0 } ? paths : null;
    }

    public string? SaveFile(SaveFileRequest request) =>
        Ask(storage => storage.SaveFilePickerAsync(SaveOptions(request)))?.TryGetLocalPath();

    public string? PickFolder(string title) =>
        Ask(storage => storage.OpenFolderPickerAsync(FolderOptions(title)))
            ?.Select(f => f.TryGetLocalPath())
            .FirstOrDefault(p => p is not null);

    internal static FilePickerOpenOptions OpenOptions(OpenFileRequest request, bool several) => new()
    {
        Title = request.Title,
        AllowMultiple = several,
        FileTypeFilter = FileTypes(request.Filters),
    };

    private static FilePickerOpenOptions StartingIn(
        IStorageProvider storage, OpenFileRequest request, FilePickerOpenOptions options)
    {
        if (request.InitialDirectory is { } folder)
            options.SuggestedStartLocation = Wait(storage.TryGetFolderFromPathAsync(folder));
        return options;
    }

    internal static FilePickerSaveOptions SaveOptions(SaveFileRequest request) => new()
    {
        Title = request.Title,
        SuggestedFileName = request.FileName,
        DefaultExtension = Extension(request),
        FileTypeChoices = FileTypes(request.Filters),
        // WPF's «Guardar» asks before writing over a file.
        ShowOverwritePrompt = true,
    };

    internal static FolderPickerOpenOptions FolderOptions(string title) => new()
    {
        Title = title,
        AllowMultiple = false,
    };

    /// <summary>
    /// The «Tipo» list, in the order it comes. The Mac takes the extensions
    /// out of the patterns.
    /// </summary>
    internal static IReadOnlyList<FilePickerFileType> FileTypes(IReadOnlyList<FileFilter> filters) =>
        filters.Select(f => new FilePickerFileType(f.Name) { Patterns = f.Patterns }).ToList();

    /// <summary>
    /// What a name typed without an extension gets, without the dot: the one
    /// asked for, or else the first filter's (Windows takes it from the
    /// filter chosen). Null when nothing has to be added.
    /// </summary>
    internal static string? Extension(SaveFileRequest request)
    {
        if (!request.AddExtension)
            return null;

        var extension = request.DefaultExtension
            ?? request.Filters.FirstOrDefault()?.Patterns.FirstOrDefault(p => p.StartsWith("*.", StringComparison.Ordinal));
        extension = extension?.TrimStart('*').TrimStart('.');
        return string.IsNullOrEmpty(extension) || extension.Contains('*') ? null : extension;
    }

    /// <summary>Waits for an answer that comes later, with the screen alive.</summary>
    internal static T Wait<T>(Task<T> task)
    {
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(
                _ => Dispatcher.UIThread.Post(() => frame.Continue = false),
                TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Opens a dialog over the window in front and waits for it. Null when
    /// there is no window to put it over or the system could not open it,
    /// which the program takes as a cancel.
    /// </summary>
    private static T? Ask<T>(Func<IStorageProvider, Task<T>> open) where T : class?
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.Invoke(() => Ask(open));

        if (Modal.Owner is not { } owner)
        {
            Log.Warning("No hay ventana donde abrir el diálogo de archivos");
            return null;
        }

        try
        {
            return Wait(open(TopLevel.GetTopLevel(owner)!.StorageProvider));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo abrir el diálogo de archivos");
            return null;
        }
    }
}
