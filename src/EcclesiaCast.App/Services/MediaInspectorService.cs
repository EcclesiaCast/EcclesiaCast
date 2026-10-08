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

    public MediaTabRule? EditTabRule(string tab, MediaTabRule current)
    {
        var window = new MediaTabRuleWindow(tab, current)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? window.Rule : null;
    }
}
