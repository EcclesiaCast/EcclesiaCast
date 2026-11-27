using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaTrashDialog(ISongRepository songs, IThemeRepository themes) : ITrashDialog
{
    public void Show() => Modal.Run(new TrashWindow(new TrashViewModel(songs, themes)));
}
