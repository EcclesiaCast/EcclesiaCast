using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Services;

public sealed class AvaloniaSongEditor(IThemeRepository themes) : ISongEditor
{
    public Song? Edit(Song? existing)
    {
        var window = new SongEditorWindow(existing, themes.GetAll());
        Modal.Run(window);
        return window.Result;
    }
}
