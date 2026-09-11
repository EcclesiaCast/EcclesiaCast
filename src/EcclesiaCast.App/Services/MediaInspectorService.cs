using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Media;

namespace EcclesiaCast.App.Services;

public sealed class MediaInspectorService : IMediaInspector
{
    public bool Edit(MediaItem item, IReadOnlyList<string> categories, IReadOnlyList<MediaItem> library)
    {
        var window = new MediaInspectorWindow(item, categories, library)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true && window.Saved;
    }
}
