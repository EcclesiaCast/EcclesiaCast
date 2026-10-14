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

    public MediaTabRule? EditTabRule(string tab, MediaTabRule current, ref bool presentation)
    {
        var window = new MediaTabRuleWindow(tab, current, presentation)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        if (window.ShowDialog() != true)
            return null;

        presentation = window.Presentation;
        return window.Rule;
    }
}
