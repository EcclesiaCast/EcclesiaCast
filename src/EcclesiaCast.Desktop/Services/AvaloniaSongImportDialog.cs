using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaSongImportDialog(
    SongSaver songSaver, IPlatformPaths paths, IDialogs dialogs, IShell shell, IPlatformInfo platform)
    : ISongImportDialog
{
    public string? Show()
    {
        var viewModel = new SongImportViewModel(songSaver, paths, platform.System);
        var window = new SongImportWindow(viewModel, dialogs, shell);
        Modal.Run(window);
        return window.Accepted ? viewModel.Summary : null;
    }
}
